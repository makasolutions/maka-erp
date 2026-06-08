// Dígito de verificación (DV) del NIT colombiano — algoritmo DIAN.
// Pesos aplicados de derecha a izquierda; suma mod 11.
const WEIGHTS = [3, 7, 13, 17, 19, 23, 29, 37, 41, 43, 47, 53, 59, 67, 71];

/** Returns the NIT verification digit for a numeric identification string, or null if invalid. */
export function nitVerificationDigit(identification: string | null | undefined): number | null {
  if (!identification) return null;
  const digits = identification.replace(/\D/g, "");
  if (!digits) return null;
  let sum = 0;
  for (let i = 0; i < digits.length; i++) {
    const d = Number(digits[digits.length - 1 - i]);
    sum += d * (WEIGHTS[i] ?? 0);
  }
  const rem = sum % 11;
  return rem < 2 ? rem : 11 - rem;
}
