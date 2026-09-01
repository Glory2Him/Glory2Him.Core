import { useState } from 'react';
import { SharingPanel } from '../../../components/contentItems/sharingPanel';
import { useDocumentTitle } from '../../useDocumentTitle';

import {
    CodeSample,
    ComponentDoc,
    ComponentPropRow,
    DocSection,
    LiveDemo,
    PropsTable
} from './shared/componentDoc';

const minimalSample = `
import { SharingPanel } from '../../components/contentItems/sharingPanel';

// The defaults are the design's own wording — a page that wants the standard
// invitation passes nothing but the destination.
<SharingPanel
    onSubmit={() => navigate('/posts/contribute', { state: { from } })} />

// Every string is a prop for a page with something more specific to say.
<SharingPanel
    iconCss="bi bi-chat-heart"
    title="Was this a help to you?"
    description="Tell us how it landed — your story might carry somebody else."
    buttonText="Share your story"
    onSubmit={() => navigate('/posts/contribute', { state: { from } })} />
`;

const adaptationSample = `
/* The panel measures ITSELF, not the viewport, with a CSS container query:

       .g2h-sharing-panel        { container-type: inline-size; }
       .g2h-sharing-panel-body   { … column, button below … }
       @container (min-width: 40rem)
       .g2h-sharing-panel-body   { … one row, button on the right … }

   So the same component wears two faces with no prop and no JS:

   WIDE  (a banner above a feed)          NARROW (a sidebar column)
   ┌────────────────────────────────┐     ┌──────────────────┐
   │ ✏️ Have something to share?     │     │ ✏️ Have something │
   │ A quote, a story…    [Submit →]│     │ to share?        │
   └────────────────────────────────┘     │ A quote, a       │
                                          │ story…           │
   The icon sits INLINE in the heading,   │ [Submit →]       │
   so a narrow title wraps to beneath     └──────────────────┘
   it; the button's own text never wraps. */
`;

const propRows: ReadonlyArray<ComponentPropRow> = [
    {
        name: 'iconCss',
        type: 'string',
        defaultValue: "'bi bi-pencil-square'",
        description: 'The css the icon renders with — any icon-font class the page’s '
            + 'stylesheets carry (Bootstrap Icons, Font Awesome). It sits inline in the '
            + 'heading, which is what lets a narrow title wrap to underneath it.'
    },
    {
        name: 'title',
        type: 'string',
        defaultValue: "'Have something to share?'",
        description: 'The heading.'
    },
    {
        name: 'description',
        type: 'string',
        defaultValue: "'A quote, a story, a testimony, or a verse that carried you through — '"
            + " + 'if it might encourage someone else, we would love to read it.'",
        description: 'The body copy. Shares a row with the button when the panel is wide; '
            + 'stacks above it when it is narrow.'
    },
    {
        name: 'buttonText',
        type: 'string',
        defaultValue: "'Submit a contribution'",
        description: 'The button’s label. It NEVER wraps — the design’s one hard rule '
            + 'about it — so a very long label widens the button rather than folding.'
    },
    {
        name: 'onSubmit',
        type: '() => void',
        description: 'Raised when the button is pressed. The panel is presentation only, so the '
            + 'CONSUMER navigates — /posts/contribute on every page that ships one, with the '
            + 'origin in router state so the contribute page can offer a way back.'
    },
    {
        name: 'cssClass',
        type: 'string',
        defaultValue: "'mb-4'",
        description: 'Appended to the panel’s own classes, for spacing in whatever it sits in.'
    }
];

export function SharingPanelDoc() {
    useDocumentTitle('Sharing Panel — Components — Glory 2 Him');

    const [lastEvent, setLastEvent] = useState('—');

    return (
        <ComponentDoc
            name="Sharing Panel"
            filePath="src/components/contentItems/sharingPanel.tsx"
            summary={
                <>
                    The invitation to contribute: icon, title, description and the way in. A
                    pure presentation component that <strong>adapts to its container</strong> —
                    a banner in a wide space, a stacked card in a sidebar — and raises{' '}
                    <code>onSubmit</code> for the page to route.
                </>
            }>

            <DocSection
                title="Usage"
                lead={
                    <>
                        The defaults are the design&rsquo;s own wording, so the common case
                        passes nothing but the destination. Where it leads is the{' '}
                        <strong>page&rsquo;s</strong> decision — every shipped consumer
                        navigates to <code>/posts/contribute</code> and carries the origin in
                        router state, the same back-button contract the search panel family
                        keeps.
                    </>
                }>
                <CodeSample code={minimalSample} />
            </DocSection>

            <DocSection title="Props">
                <PropsTable rows={propRows} />
            </DocSection>

            <DocSection
                title="It adapts to its container, not the viewport"
                lead={
                    <>
                        A CSS container query keys the layout off the panel&rsquo;s{' '}
                        <em>own</em> width, so the same component serves a full-width slot and
                        a sidebar without a prop telling it which it is in. Wide: the
                        description and the button share a row, button on the right. Narrow:
                        they stack, the heading wraps to beneath its inline icon, and the
                        button text still refuses to wrap.
                    </>
                }>
                <CodeSample code={adaptationSample} />

                <LiveDemo title="Live — wide (the banner face)">
                    <SharingPanel
                        cssClass="mb-0"
                        onSubmit={() => setLastEvent('onSubmit() — wide')} />
                </LiveDemo>

                <LiveDemo title="Live — narrow (the sidebar face)">
                    {/* The demo narrows the CONTAINER, which is all the panel ever looks at —
                        resize the window and this one does not change face, because its box
                        does not. */}
                    <div style={{ maxWidth: '20rem' }}>
                        <SharingPanel
                            cssClass="mb-0"
                            onSubmit={() => setLastEvent('onSubmit() — narrow')} />
                    </div>
                </LiveDemo>

                <p className="small text-body-secondary">
                    Last event: <code>{lastEvent}</code>
                </p>
            </DocSection>

            <DocSection
                title="Every string is a prop"
                lead={
                    <>
                        A surface with something more specific to ask swaps the wording and the
                        icon and keeps the behaviour.
                    </>
                }>
                <LiveDemo title="Live — reworded">
                    <SharingPanel
                        cssClass="mb-0"
                        iconCss="bi bi-chat-heart"
                        title="Was this a help to you?"
                        description="Tell us how it landed — your story might carry somebody else."
                        buttonText="Share your story"
                        onSubmit={() => setLastEvent('onSubmit() — reworded')} />
                </LiveDemo>
            </DocSection>

            <DocSection
                title="What it deliberately leaves out"
                lead={
                    <>
                        <strong>Navigation</strong> — the button raises an event and the page
                        routes it, so the same panel serves any surface whatever &ldquo;go
                        contribute&rdquo; means there. <strong>Authentication</strong> — the
                        contribute page already answers a signed-out arrival with its login
                        prompt, so this panel does not ask who is looking. There is no{' '}
                        <code>useQuery</code>, no <code>useMutation</code> and no broker call
                        inside.
                    </>
                } />
        </ComponentDoc>
    );
}
