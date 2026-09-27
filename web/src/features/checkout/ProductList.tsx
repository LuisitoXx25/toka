import type { Product } from '../../api/types'
import { formatMoney } from '../../lib/format'

interface Props {
  products: Product[]
  selectedId: string | null
  onSelect: (id: string) => void
  disabled: boolean
}

export function ProductList({ products, selectedId, onSelect, disabled }: Props) {
  return (
    <fieldset className="product-list" disabled={disabled}>
      <legend className="visually-hidden">Producto</legend>
      {products.map((product) => {
        const soldOut = product.stock === 0
        return (
          <label key={product.id} className={`product ${selectedId === product.id ? 'product--selected' : ''} ${soldOut ? 'product--soldout' : ''}`}>
            <input
              type="radio"
              name="product"
              value={product.id}
              checked={selectedId === product.id}
              disabled={soldOut}
              onChange={() => onSelect(product.id)}
            />
            <span className="product__info">
              <span className="product__name">{product.name}</span>
              <span className="product__meta">{product.description}</span>
              <span className="product__meta">
                SKU {product.sku} · {soldOut ? 'Agotado' : `${product.stock} disponibles`}
              </span>
            </span>
            <span className="amount product__price">{formatMoney(product.price, product.currency)}</span>
          </label>
        )
      })}
    </fieldset>
  )
}

export function ProductListSkeleton() {
  return (
    <div className="product-list" aria-busy="true" aria-label="Cargando catálogo">
      {[0, 1, 2].map((i) => (
        <div key={i} className="product product--skeleton">
          <span className="skeleton skeleton--radio" />
          <span className="product__info">
            <span className="skeleton skeleton--line" />
            <span className="skeleton skeleton--line skeleton--short" />
          </span>
          <span className="skeleton skeleton--price" />
        </div>
      ))}
    </div>
  )
}
