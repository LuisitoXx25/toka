import { ToastProvider } from './components/Toast'
import { CheckoutPage } from './features/checkout/CheckoutPage'
import { PurchasesPage } from './features/purchases/PurchasesPage'
import { navigate, usePath } from './lib/router'

export default function App() {
  const path = usePath()
  const purchasesMatch = /^\/compras(?:\/([^/]+))?\/?$/.exec(path)

  return (
    <ToastProvider>
      <a className="skip-link" href="#main">Ir al contenido</a>
      <header className="topbar">
        <nav className="topbar__nav" aria-label="Principal">
          <NavLink to="/" active={!purchasesMatch}>Comprar</NavLink>
          <NavLink to="/compras" active={Boolean(purchasesMatch)}>Mis compras</NavLink>
        </nav>
      </header>
      <main id="main" className="container">
        {purchasesMatch
          ? <PurchasesPage key={purchasesMatch[1] ?? ''} orderId={purchasesMatch[1] ?? null} />
          : <CheckoutPage />}
      </main>
    </ToastProvider>
  )
}

function NavLink({ to, active, children }: { to: string; active: boolean; children: string }) {
  return (
    <a href={to} aria-current={active ? 'page' : undefined}
      onClick={(e) => { e.preventDefault(); navigate(to) }}>
      {children}
    </a>
  )
}
