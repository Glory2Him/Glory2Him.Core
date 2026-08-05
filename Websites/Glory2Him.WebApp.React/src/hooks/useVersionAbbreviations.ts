import { useMemo } from 'react';
import { useVersions } from '@youversion/platform-react-hooks';
import {
    resolveVersionAbbreviation,
    resolveVersionId,
} from '../models/youVersion/youVersionVersions';

// Version abbreviations for the URL (/BibleReferences/JHN.3.16.NIV), resolved from the
// platform's own catalogue so every translation round-trips — not just the handful named in
// youVersionVersions. The catalogue is fetched once with two fields and cached by the SDK;
// until it arrives (and if it fails) the small built-in map answers, so the common versions
// work from the first render.
export interface VersionAbbreviations {
    abbreviationFor: (versionId: number) => string | null;
    versionIdFor: (abbreviation: string | null | undefined) => number;
}

export function useVersionAbbreviations(): VersionAbbreviations {
    const { versions } = useVersions(undefined, undefined, {
        page_size: '*',
        fields: ['id', 'abbreviation'],
    });

    return useMemo(() => {
        const abbreviationsById = new Map<number, string>();
        const idsByAbbreviation = new Map<string, number>();

        for (const version of versions?.data ?? []) {
            const abbreviation = version.abbreviation?.toUpperCase();

            if (abbreviation == null || abbreviation.length === 0) {
                continue;
            }

            // The catalogue lists several editions under one abbreviation; the first wins,
            // which keeps a URL pointing at the same text between visits.
            if (!abbreviationsById.has(version.id)) {
                abbreviationsById.set(version.id, abbreviation);
            }

            if (!idsByAbbreviation.has(abbreviation)) {
                idsByAbbreviation.set(abbreviation, version.id);
            }
        }

        return {
            abbreviationFor: (versionId: number) =>
                abbreviationsById.get(versionId) ?? resolveVersionAbbreviation(versionId),

            versionIdFor: (abbreviation: string | null | undefined) => {
                if (abbreviation == null || abbreviation.length === 0) {
                    return resolveVersionId(undefined);
                }

                return idsByAbbreviation.get(abbreviation.toUpperCase())
                    ?? resolveVersionId(abbreviation);
            },
        };
    }, [versions]);
}
