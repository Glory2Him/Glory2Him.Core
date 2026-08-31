import { ContentItemFormPanel, ContentItemFormPanelProps } from './contentItemFormPanel';

// THE ADD TEMPLATE of the ContentItemPanel family: the type picker and a blank form. It IS the
// form engine with no item — restated as its own component so the family tree names the surface
// (ContentItemPanel dispatches here when it is handed a settings collection and no item), and so
// a page that wants the deep text/role overrides can render the template directly.
export type ContentItemAddPanelProps = Omit<ContentItemFormPanelProps, 'contentItem'>;

export function ContentItemAddPanel(props: ContentItemAddPanelProps) {
    return <ContentItemFormPanel {...props} contentItem={undefined} />;
}
