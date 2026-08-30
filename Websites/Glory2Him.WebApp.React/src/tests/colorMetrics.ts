// Colour maths for the tests that hold a palette to being legible and to being TELLABLE APART.
// Test-only: nothing in the app bundle imports this.
//
// Two questions get asked of a colour pair, and they are not the same question. Contrast (WCAG)
// asks whether text can be READ on a fill — a light and a dark blue pass it easily while looking
// identical. Perceptual distance (CIEDE2000) asks whether two fills look like different colours at
// all, which is the question a palette that encodes meaning has to answer.

type Rgb = readonly [number, number, number];
type Lab = readonly [number, number, number];

const hexToRgb = (hex: string): Rgb => {
    const value = hex.replace('#', '');

    return [
        parseInt(value.slice(0, 2), 16),
        parseInt(value.slice(2, 4), 16),
        parseInt(value.slice(4, 6), 16)
    ];
};

// The sRGB transfer function, undone. Averaging or comparing gamma-encoded channels directly is
// the classic way to get colour maths subtly wrong.
const toLinear = (channel: number): number => {
    const scaled = channel / 255;

    return scaled <= 0.04045
        ? scaled / 12.92
        : Math.pow((scaled + 0.055) / 1.055, 2.4);
};

const relativeLuminance = (hex: string): number => {
    const [red, green, blue] = hexToRgb(hex).map(toLinear);

    return 0.2126 * red + 0.7152 * green + 0.0722 * blue;
};

// WCAG 2.x contrast ratio, 1:1 (identical) to 21:1 (black on white). AA wants 4.5 for body text.
export const contrastRatio = (first: string, second: string): number => {
    const firstLuminance = relativeLuminance(first);
    const secondLuminance = relativeLuminance(second);

    const lighter = Math.max(firstLuminance, secondLuminance);
    const darker = Math.min(firstLuminance, secondLuminance);

    return (lighter + 0.05) / (darker + 0.05);
};

const hexToLab = (hex: string): Lab => {
    const [red, green, blue] = hexToRgb(hex).map(toLinear);

    const x = red * 0.4124564 + green * 0.3575761 + blue * 0.1804375;
    const y = red * 0.2126729 + green * 0.7151522 + blue * 0.0721750;
    const z = red * 0.0193339 + green * 0.1191920 + blue * 0.9503041;

    // D65 reference white, which is what sRGB is defined against.
    const whiteX = 0.95047;
    const whiteY = 1.0;
    const whiteZ = 1.08883;

    const delta = 6 / 29;

    const f = (t: number): number => t > delta ** 3
        ? Math.cbrt(t)
        : t / (3 * delta ** 2) + 4 / 29;

    const fx = f(x / whiteX);
    const fy = f(y / whiteY);
    const fz = f(z / whiteZ);

    return [116 * fy - 16, 500 * (fx - fy), 200 * (fy - fz)];
};

const toDegrees = (radians: number): number => radians * 180 / Math.PI;
const toRadians = (degrees: number): number => degrees * Math.PI / 180;

// CIEDE2000. Roughly: under 1 is invisible, under ~5 reads as the same colour with a slightly
// different shade, and by ~20 nobody would call them the same colour.
//
// The formula is transcribed rather than derived, so it is written out plainly and its terms keep
// the names the standard gives them. Verified against the reference pairs in the test beside it.
export const perceptualDistance = (firstHex: string, secondHex: string): number => {
    const [l1, a1, b1] = hexToLab(firstHex);
    const [l2, a2, b2] = hexToLab(secondHex);

    const c1 = Math.hypot(a1, b1);
    const c2 = Math.hypot(a2, b2);
    const cBar = (c1 + c2) / 2;

    const g = 0.5 * (1 - Math.sqrt(cBar ** 7 / (cBar ** 7 + 25 ** 7)));

    const a1Prime = (1 + g) * a1;
    const a2Prime = (1 + g) * a2;

    const c1Prime = Math.hypot(a1Prime, b1);
    const c2Prime = Math.hypot(a2Prime, b2);

    const hueOf = (a: number, b: number): number =>
        a === 0 && b === 0 ? 0 : (toDegrees(Math.atan2(b, a)) + 360) % 360;

    const h1Prime = hueOf(a1Prime, b1);
    const h2Prime = hueOf(a2Prime, b2);

    const deltaLPrime = l2 - l1;
    const deltaCPrime = c2Prime - c1Prime;

    let deltaHue = 0;

    if (c1Prime * c2Prime !== 0) {
        deltaHue = h2Prime - h1Prime;

        if (deltaHue > 180) {
            deltaHue -= 360;
        } else if (deltaHue < -180) {
            deltaHue += 360;
        }
    }

    const deltaHPrime =
        2 * Math.sqrt(c1Prime * c2Prime) * Math.sin(toRadians(deltaHue) / 2);

    const lBarPrime = (l1 + l2) / 2;
    const cBarPrime = (c1Prime + c2Prime) / 2;

    let hBarPrime: number;

    if (c1Prime * c2Prime === 0) {
        hBarPrime = h1Prime + h2Prime;
    } else if (Math.abs(h1Prime - h2Prime) <= 180) {
        hBarPrime = (h1Prime + h2Prime) / 2;
    } else if (h1Prime + h2Prime < 360) {
        hBarPrime = (h1Prime + h2Prime + 360) / 2;
    } else {
        hBarPrime = (h1Prime + h2Prime - 360) / 2;
    }

    const t = 1
        - 0.17 * Math.cos(toRadians(hBarPrime - 30))
        + 0.24 * Math.cos(toRadians(2 * hBarPrime))
        + 0.32 * Math.cos(toRadians(3 * hBarPrime + 6))
        - 0.20 * Math.cos(toRadians(4 * hBarPrime - 63));

    const deltaTheta = 30 * Math.exp(-(((hBarPrime - 275) / 25) ** 2));
    const rc = 2 * Math.sqrt(cBarPrime ** 7 / (cBarPrime ** 7 + 25 ** 7));

    const sl = 1
        + (0.015 * (lBarPrime - 50) ** 2) / Math.sqrt(20 + (lBarPrime - 50) ** 2);

    const sc = 1 + 0.045 * cBarPrime;
    const sh = 1 + 0.015 * cBarPrime * t;
    const rt = -Math.sin(toRadians(2 * deltaTheta)) * rc;

    const lightnessTerm = deltaLPrime / sl;
    const chromaTerm = deltaCPrime / sc;
    const hueTerm = deltaHPrime / sh;

    return Math.sqrt(
        lightnessTerm ** 2
        + chromaTerm ** 2
        + hueTerm ** 2
        + rt * chromaTerm * hueTerm);
};
