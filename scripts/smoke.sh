#!/usr/bin/env bash
# End-to-end smoke test against the running docker compose stack, through the public nginx entry point
# (the same path the browser uses; no API key is sent from here).
# Usage: ./scripts/smoke.sh [base-url]   (default http://localhost:5173)
set -euo pipefail

BASE="${1:-http://localhost:5173}"
API="$BASE/api/v1"
HEADPHONES="0199a0d4-0000-7000-8000-000000000002"
EMAIL="smoke.$(date +%s)@correo.mx"
failures=0

check() { # description expected actual
  if [ "$2" = "$3" ]; then printf '  ok    %s\n' "$1"; else printf '  FALLA %s (esperado %s, obtenido %s)\n' "$1" "$2" "$3"; failures=$((failures + 1)); fi
}
status() { curl -s -o /dev/null -w '%{http_code}' "$@"; }
json() { python3 -c "import sys,json; d=json.load(sys.stdin); print($1)"; }
order() { # card installments
  curl -s -X POST "$API/orders" -H 'Content-Type: application/json' -H "Idempotency-Key: smoke-$RANDOM-$RANDOM" -d "{
    \"customer\": {\"firstName\": \"Ana\", \"lastName\": \"López\", \"email\": \"$EMAIL\"},
    \"productId\": \"$HEADPHONES\", \"quantity\": 1, \"installments\": $2,
    \"card\": {\"holderName\": \"ANA LOPEZ\", \"number\": \"$1\", \"expiryMonth\": 12, \"expiryYear\": 2030, \"cvv\": \"123\"}}"
}

echo "Catálogo y formas de pago"
check "GET productos" 200 "$(status "$API/products")"
check "3 y 6 MSI con crédito" "[1, 3, 6]" "$(curl -s "$API/installment-plans?amount=3499&bin=411111" | json '[p["months"] for p in d["plans"]]')"
check "débito solo contado" "[1]" "$(curl -s "$API/installment-plans?amount=3499&bin=400005" | json '[p["months"] for p in d["plans"]]')"

echo "Checkout"
paid=$(order 4111111111111111 3)
check "pago aprobado a 3 MSI" "Paid" "$(echo "$paid" | json 'd["status"]')"
check "el primer pago absorbe el redondeo" "3 pagos de \$1,166.33 sin intereses; el primero de \$1,166.34" "$(echo "$paid" | json 'd["paymentPlan"]')"
check "IVA desglosado" "3016.38 + 482.62 = 3499.0" "$(echo "$paid" | json '"%s + %s = %s" % (d["subtotal"], d["taxAmount"], d["total"])')"
declined=$(order 4000000000000002 1)
declined_id=$(echo "$declined" | json 'd["id"]')
check "tarjeta rechazada" "PaymentDeclined" "$(echo "$declined" | json 'd["status"]')"
check "reintento automático tras error temporal" "TransientError,Approved" "$(order 4000000000000259 1 | json '",".join(a["outcome"] for a in d["attempts"])')"
check "débito con MSI rechazado" "installments_not_available" "$(order 4000056655665556 3 | json 'd.get("code")')"

echo "Consulta y reintento"
# Bodies are built first: inside $(...) bash would brace-expand an inline {"a":1,"b":2} into two arguments.
owner_lookup=$(printf '{"orderId":"%s","email":"%s"}' "$declined_id" "$EMAIL")
stranger_lookup=$(printf '{"orderId":"%s","email":"otra@correo.mx"}' "$declined_id")
retry_body=$(printf '{"email":"%s","card":{"holderName":"ANA","number":"5555555555554444","expiryMonth":1,"expiryYear":2031,"cvv":"321"}}' "$EMAIL")
check "consulta con el correo del comprador" 200 "$(status -X POST "$API/orders/lookup" -H 'Content-Type: application/json' -d "$owner_lookup")"
check "consulta con otro correo" 404 "$(status -X POST "$API/orders/lookup" -H 'Content-Type: application/json' -d "$stranger_lookup")"
check "reintento con otra tarjeta" "Paid" "$(curl -s -X POST "$API/orders/$declined_id/payment-retries" -H 'Content-Type: application/json' -d "$retry_body" | json 'd["status"]')"

echo "Superficie expuesta por el proxy"
check "clientes (datos personales) no expuestos" 404 "$(status "$API/customers/$HEADPHONES")"
check "orden por id sin correo no expuesta" 404 "$(status "$API/orders/$declined_id")"
check "método no permitido" 403 "$(status -X DELETE "$API/products")"
check "CSP presente" 1 "$(curl -sI "$BASE/" | grep -ci '^content-security-policy')"
key=$(grep '^API_KEY=' .env 2>/dev/null | cut -d= -f2 || true)
if [ -n "$key" ]; then
  js=$(curl -s "$BASE/" | grep -oE '/assets/[^"]+\.js' | head -1)
  check "la API key no está en el bundle" 0 "$(curl -s "$BASE$js" | grep -c "$key" || true)"
fi

echo
if [ "$failures" -eq 0 ]; then echo "Todas las verificaciones pasaron."; else echo "$failures verificación(es) fallaron."; exit 1; fi
