import { ContentItemFormItem } from '../../models/components/contentItems/contentItemFormItem';
import { ContentItemFormPanel, ContentItemFormPanelProps } from './contentItemFormPanel';

// THE EDIT TEMPLATE of the ContentItemPanel family: the frozen type and the seeded form, with
// removal riding on it. It IS the form engine with an item — restated as its own component so
// the family tree names the surface (ContentItemPanel dispatches here when its Edit affordance
// is taken in place), and so a page can land straight on an editor when that is the ask.
export type ContentItemEditPanelProps =
    Omit<ContentItemFormPanelProps, 'contentItem'> & { contentItem: ContentItemFormItem };

export function ContentItemEditPanel(props: ContentItemEditPanelProps) {
    return <ContentItemFormPanel {...props} />;
}
