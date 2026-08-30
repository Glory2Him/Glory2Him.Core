import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { contrastRatio, perceptualDistance } from '../../tests/colorMetrics';

import {
    ContentType,
    contentTypeMembers
} from '../../models/foundations/contentItemSettings/contentType';

// THE STYLESHEET IS THE SOURCE OF TRUTH FOR A TYPE'S COLOUR, which is the whole point of keying
// the chip off data-content-type — so this reads the CSS rather than a duplicate table, and a
// palette edit is checked by the same run that ships it.
//
// Resolved from the project root rather than from import.meta.url: Vite rewrites that to a
// browser-style URL, and node:fs will not take one.
const paletteCss = readFileSync(
    resolve(process.cwd(), 'src/components/contentItems/contentItems.css'), 'utf8');

type ChipColours = { readonly background: string; readonly foreground: string };

// One block per type, naming the chip and the selected picker tile together:
//
//   .g2h-content-item-chip[data-content-type="Quote"],
//   .g2h-content-item-type-selected[data-content-type="Quote"] {
//       --g2h-chip-bg: #ba5cf2;
//       --g2h-chip-fg: #14171c;
//   }
const paletteOf = (): ReadonlyMap<string, ChipColours> => {
    const blockPattern =
        /\.g2h-content-item-chip\[data-content-type="(\w+)"\][^{]*\{([^}]*)\}/g;

    const palette = new Map<string, ChipColours>();

    for (const [, typeName, body] of paletteCss.matchAll(blockPattern)) {
        const background = /--g2h-chip-bg:\s*(#[0-9a-fA-F]{6})/.exec(body)?.[1];
        const foreground = /--g2h-chip-fg:\s*(#[0-9a-fA-F]{6})/.exec(body)?.[1];

        if (background != null && foreground != null) {
            palette.set(typeName, { background, foreground });
        }
    }

    return palette;
};

// Past this, two colours are not "a bit similar" — they are not the same colour. The pair that
// prompted this test (an indigo Quote against a blue Devotional) measured 13.5 and read on the
// page as one colour used twice.
const minimumDistance = 20;

// WCAG AA for body text. The chip's label is small, so the large-text 3:1 allowance is not ours.
const minimumContrast = 4.5;

// The page backgrounds a chip is ever laid on. Both dark values are listed because the theme
// overrides Bootstrap's default: measured off the running app the body is #191a1f, while a
// surface still on the stock token is #212529, and a chip has to survive either.
const pageBackgrounds: ReadonlyArray<readonly [string, string]> = [
    ['light page', '#ffffff'],
    ['dark page', '#191a1f'],
    ['stock dark page', '#212529']
];

describe('the content type chip palette', () => {
    const palette = paletteOf();

    it('should give every content type a colour of its own', () => {
        // then: a type with no block falls back to the neutral default, which is not a failure
        // anybody would SEE — it just quietly looks like every other uncoloured type. Seeding a
        // new ContentType has to fail here instead.
        const uncoloured = contentTypeMembers
            .map((contentType) => ContentType[contentType])
            .filter((typeName) => palette.has(typeName) === false);

        expect(uncoloured).toEqual([]);
    });

    it('should colour no type the stylesheet does not know about', () => {
        // then: a block left behind by a renamed or removed member is dead weight that still
        // reads as policy
        const known = contentTypeMembers.map((contentType) => ContentType[contentType]);
        const orphaned = [...palette.keys()].filter((name) => known.includes(name) === false);

        expect(orphaned).toEqual([]);
    });

    it('should keep every pair of types clearly tellable apart', () => {
        // given
        const entries = [...palette.entries()];

        // when: every pair, not just the neighbours in the file — the two that collided were
        // four rules apart, which is exactly why reading down the list did not catch it
        const tooClose: string[] = [];

        for (let first = 0; first < entries.length; first++) {
            for (let second = first + 1; second < entries.length; second++) {
                const [firstName, firstColours] = entries[first];
                const [secondName, secondColours] = entries[second];

                const distance = perceptualDistance(
                    firstColours.background, secondColours.background);

                if (distance < minimumDistance) {
                    tooClose.push(
                        `${firstName} (${firstColours.background})`
                        + ` / ${secondName} (${secondColours.background})`
                        + ` — ${distance.toFixed(1)}`);
                }
            }
        }

        // then
        expect(tooClose).toEqual([]);
    });

    it('should keep every chip tellable apart from the page it sits on', () => {
        // A chip whose fill matches the background is not a chip — it is text with padding, and
        // the colour that was carrying the type's identity has gone. A dark slate Topic measured
        // 11 against the dark theme and did exactly that.
        //
        // MEASURED PERCEPTUALLY, not by luminance contrast, because the two answer different
        // questions here. Amber on white is only 1.64:1 by luminance and yet is unmistakably a
        // chip — the hue carries it, and the label inside supplies its own contrast. Luminance
        // would have condemned the amber and cleared the slate; distance gets both right.

        // when
        const invisible: string[] = [];

        for (const [typeName, colours] of palette) {
            for (const [themeName, pageBackground] of pageBackgrounds) {
                const distance = perceptualDistance(colours.background, pageBackground);

                if (distance < minimumDistance) {
                    invisible.push(
                        `${typeName} (${colours.background})`
                        + ` on the ${themeName} page — ${distance.toFixed(1)}`);
                }
            }
        }

        // then
        expect(invisible).toEqual([]);
    });

    it('should keep every chip label readable on its own fill', () => {
        // when
        const unreadable = [...palette.entries()]
            .filter(([, colours]) =>
                contrastRatio(colours.background, colours.foreground) < minimumContrast)
            .map(([typeName, colours]) =>
                `${typeName} — ${contrastRatio(colours.background, colours.foreground).toFixed(2)}:1`);

        // then
        expect(unreadable).toEqual([]);
    });
});

// The distance function is transcribed from the CIEDE2000 standard rather than derived, so it is
// pinned to values that do not come from this codebase — otherwise a mistake in the maths would
// simply move the threshold the palette is measured against, and both would agree while being
// wrong together.
describe('perceptualDistance', () => {
    it('should call a colour identical to itself', () => {
        expect(perceptualDistance('#ba5cf2', '#ba5cf2')).toBeCloseTo(0, 5);
    });

    it('should be symmetric', () => {
        const forward = perceptualDistance('#f7c32e', '#1668dc');
        const backward = perceptualDistance('#1668dc', '#f7c32e');

        expect(forward).toBeCloseTo(backward, 10);
    });

    // The two ends of the scale, which is what the threshold above is calibrated against. Black
    // against white is the largest distance the formula can report, and two reds one 8-bit step
    // apart is the smallest anybody could be asked to see — so a transcription error that scaled
    // or flattened the result shows up here rather than quietly moving what "20" means.
    it('should place the extremes where the scale says they are', () => {
        expect(perceptualDistance('#000000', '#ffffff')).toBeGreaterThan(95);
        expect(perceptualDistance('#ff0000', '#fe0000')).toBeLessThan(1);
    });
});
