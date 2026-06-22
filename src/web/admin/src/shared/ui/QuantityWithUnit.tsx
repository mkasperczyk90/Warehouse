/**
 * QuantityWithUnit — never a bare number; always a unit, tabular numerals
 * (echoes the domain's unit-safe `Quantity`). Uses the `.qty` token class.
 */
export function QuantityWithUnit({ value, unit }: { value: number; unit: string }) {
  return (
    <span className="qty">
      {value.toLocaleString()}
      <span className="unit">{unit}</span>
    </span>
  );
}
