// International product-code validation (GS1 + ISBN check digits).
// Spec §2.9: ProductCode types SKU | EAN | UPC | ISBN | GTIN | PartNumber |
// ManufacturerCode | SupplierCode. The numeric GS1/ISBN types carry a check
// digit that we validate client-side before sending to the API.

export type ProductCodeType =
  | "SKU"
  | "EAN"
  | "UPC"
  | "ISBN"
  | "GTIN"
  | "PartNumber"
  | "ManufacturerCode"
  | "SupplierCode";

export const PRODUCT_CODE_TYPES: ProductCodeType[] = [
  "SKU",
  "EAN",
  "UPC",
  "ISBN",
  "GTIN",
  "PartNumber",
  "ManufacturerCode",
  "SupplierCode",
];

/** GS1 mod-10 check digit over the first n-1 digits (EAN-8/13, UPC-A, GTIN-14). */
function gs1Mod10Valid(digits: string): boolean {
  const n = digits.length;
  let sum = 0;
  // Weights alternate 3,1,... from the rightmost data digit (excluding check).
  for (let i = 0; i < n - 1; i++) {
    const d = digits.charCodeAt(n - 2 - i) - 48; // walk right→left over data digits
    sum += i % 2 === 0 ? d * 3 : d;
  }
  const check = (10 - (sum % 10)) % 10;
  return check === digits.charCodeAt(n - 1) - 48;
}

function isAllDigits(v: string): boolean {
  return /^[0-9]+$/.test(v);
}

function isbn10Valid(raw: string): boolean {
  const v = raw.replace(/[-\s]/g, "").toUpperCase();
  if (!/^[0-9]{9}[0-9X]$/.test(v)) return false;
  let sum = 0;
  for (let i = 0; i < 9; i++) sum += (10 - i) * (v.charCodeAt(i) - 48);
  sum += v[9] === "X" ? 10 : v.charCodeAt(9) - 48;
  return sum % 11 === 0;
}

function isbn13Valid(v: string): boolean {
  if (!/^(978|979)[0-9]{10}$/.test(v)) return false;
  return gs1Mod10Valid(v);
}

export type CodeValidation = { valid: boolean; messageKey?: string };

/**
 * Validates a code value for its type. Returns a messageKey (i18n) on failure.
 * Non-numeric types (SKU, PartNumber, Manufacturer/SupplierCode) only require
 * a non-empty, reasonable-length value.
 */
export function validateProductCode(type: ProductCodeType, rawValue: string): CodeValidation {
  const value = rawValue.trim();
  if (!value) return { valid: false, messageKey: "codes.errors.required" };

  switch (type) {
    case "EAN": {
      const v = value.replace(/[\s-]/g, "");
      if (!isAllDigits(v) || (v.length !== 13 && v.length !== 8))
        return { valid: false, messageKey: "codes.errors.ean" };
      return gs1Mod10Valid(v) ? { valid: true } : { valid: false, messageKey: "codes.errors.checkDigit" };
    }
    case "UPC": {
      const v = value.replace(/[\s-]/g, "");
      if (!isAllDigits(v) || v.length !== 12)
        return { valid: false, messageKey: "codes.errors.upc" };
      return gs1Mod10Valid(v) ? { valid: true } : { valid: false, messageKey: "codes.errors.checkDigit" };
    }
    case "GTIN": {
      const v = value.replace(/[\s-]/g, "");
      if (!isAllDigits(v) || ![8, 12, 13, 14].includes(v.length))
        return { valid: false, messageKey: "codes.errors.gtin" };
      return gs1Mod10Valid(v) ? { valid: true } : { valid: false, messageKey: "codes.errors.checkDigit" };
    }
    case "ISBN": {
      const v = value.replace(/[\s-]/g, "");
      if (v.length === 10) return isbn10Valid(v) ? { valid: true } : { valid: false, messageKey: "codes.errors.isbn" };
      if (v.length === 13) return isbn13Valid(v) ? { valid: true } : { valid: false, messageKey: "codes.errors.isbn" };
      return { valid: false, messageKey: "codes.errors.isbn" };
    }
    case "SKU":
      if (value.length > 64) return { valid: false, messageKey: "codes.errors.tooLong" };
      return { valid: true };
    default:
      // PartNumber / ManufacturerCode / SupplierCode — free-form, length-capped.
      if (value.length > 128) return { valid: false, messageKey: "codes.errors.tooLong" };
      return { valid: true };
  }
}
