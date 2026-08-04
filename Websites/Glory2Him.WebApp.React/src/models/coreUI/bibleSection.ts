import { BibleVerse } from './bibleVerse';

export interface BibleSection {
    heading?: string;
    verses: ReadonlyArray<BibleVerse>;
}
