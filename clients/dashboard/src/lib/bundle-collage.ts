// Builds a square collage (PNG blob) from a set of product image URLs, drawn
// client-side on a <canvas>. Used by the Bundle step to auto-generate a combo
// cover image from the included products' primary images.

const SIZE = 1000; // output canvas is SIZE×SIZE px
const GAP = 12;
const BG = "#ffffff";

function loadImage(url: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.crossOrigin = "anonymous"; // required so the canvas isn't tainted on toBlob()
    img.onload = () => resolve(img);
    img.onerror = () => reject(new Error(`No se pudo cargar la imagen: ${url}`));
    img.src = url;
  });
}

/** Grid dimensions (cols×rows) that best fit `n` cells in a square. */
function gridFor(n: number): { cols: number; rows: number } {
  if (n <= 1) return { cols: 1, rows: 1 };
  if (n === 2) return { cols: 2, rows: 1 };
  if (n <= 4) return { cols: 2, rows: 2 };
  if (n <= 6) return { cols: 3, rows: 2 };
  if (n <= 9) return { cols: 3, rows: 3 };
  const cols = Math.ceil(Math.sqrt(n));
  return { cols, rows: Math.ceil(n / cols) };
}

/** Draw `img` into the target box using object-fit: cover semantics. */
function drawCover(
  ctx: CanvasRenderingContext2D,
  img: HTMLImageElement,
  x: number,
  y: number,
  w: number,
  h: number,
): void {
  const scale = Math.max(w / img.width, h / img.height);
  const dw = img.width * scale;
  const dh = img.height * scale;
  const dx = x + (w - dw) / 2;
  const dy = y + (h - dh) / 2;
  ctx.save();
  ctx.beginPath();
  ctx.rect(x, y, w, h);
  ctx.clip();
  ctx.drawImage(img, dx, dy, dw, dh);
  ctx.restore();
}

/**
 * Generate a collage from up to 9 image URLs. Returns a PNG Blob.
 * Throws if no image could be loaded (e.g. CORS-tainted) so the caller can toast.
 */
export async function buildBundleCollage(urls: string[]): Promise<Blob> {
  const unique = Array.from(new Set(urls.filter(Boolean))).slice(0, 9);
  if (unique.length === 0) throw new Error("No hay imágenes para combinar.");

  const loaded = (await Promise.allSettled(unique.map(loadImage)))
    .filter((r): r is PromiseFulfilledResult<HTMLImageElement> => r.status === "fulfilled")
    .map((r) => r.value);
  if (loaded.length === 0) throw new Error("Ninguna imagen pudo cargarse (posible bloqueo CORS).");

  const { cols, rows } = gridFor(loaded.length);
  const canvas = document.createElement("canvas");
  canvas.width = SIZE;
  canvas.height = SIZE;
  const ctx = canvas.getContext("2d");
  if (!ctx) throw new Error("Canvas 2D no disponible.");

  ctx.fillStyle = BG;
  ctx.fillRect(0, 0, SIZE, SIZE);

  const cellW = (SIZE - GAP * (cols + 1)) / cols;
  const cellH = (SIZE - GAP * (rows + 1)) / rows;

  loaded.forEach((img, i) => {
    const c = i % cols;
    const r = Math.floor(i / cols);
    const x = GAP + c * (cellW + GAP);
    const y = GAP + r * (cellH + GAP);
    drawCover(ctx, img, x, y, cellW, cellH);
  });

  return await new Promise<Blob>((resolve, reject) => {
    canvas.toBlob(
      (blob) => (blob ? resolve(blob) : reject(new Error("No se pudo exportar el collage."))),
      "image/png",
    );
  });
}
