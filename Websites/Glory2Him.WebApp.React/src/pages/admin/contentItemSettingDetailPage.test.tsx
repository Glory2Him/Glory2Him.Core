import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ContentItemSetting } from '../../models/foundations/contentItemSettings/contentItemSetting';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';
import { ContentItemSettingDetailPage } from './contentItemSettingDetailPage';

// WHERE THE PAGE LETS GO. Both exits — Back and a successful save — return to the view the
// administrator was working through, which the list handed over in router state when Manage was
// taken. The editing itself is the page's other subject; what is asserted here is the way out.
let setting: ContentItemSetting | null = null;
let isLoadingSetting = false;
let isErrorLoadingSetting = false;
let saveOutcome: 'succeeds' | 'refuses' = 'succeeds';
const saved = vi.fn();

vi.mock('../../services/foundations/contentItemSettingService', () => ({
    contentItemSettingService: {
        useGetContentItemSettingById: () => ({
            data: setting,
            isLoading: isLoadingSetting,
            isError: isErrorLoadingSetting
        }),

        useUpdateContentItemSetting: () => ({
            isPending: false,

            mutateAsync: async (updated: ContentItemSetting) => {
                saved(updated);

                if (saveOutcome === 'refuses') {
                    throw new Error('refused');
                }

                return updated;
            }
        })
    }
}));

const settingId = '11111111-1111-1111-1111-111111111111';
const settingsRoute = '/Admin/ContentItemSettings';
const filteredRoute = `${settingsRoute}?q=verse&type=Quote&scope=Default&page=2`;

const createSetting = (overrides: Partial<ContentItemSetting> = {}): ContentItemSetting => ({
    id: settingId,
    contentType: ContentType.Quote,
    contentItemId: null,
    contentTypeName: 'Quote',
    contentTypeDescription: 'Words that stirred you',
    contentTypeIconCssClass: 'bi-quote',
    sortOrder: 0,
    hasTitle: false,
    hasAuthor: true,
    isAvailableAsGeneralUserContribution: true,
    tagsAllowed: true,
    showTags: true,
    reactionsAllowed: true,
    showReactions: true,
    linksAllowed: false,
    showLinks: true,
    attachmentsAllowed: false,
    showAttachments: true,
    commentsAllowed: true,
    showComments: true,
    bibleReferenceAllowed: true,
    showBibleReferences: true,
    limitReactionsToLoveOnly: false,
    createdBy: 'system-seed',
    createdWhen: '2026-08-28T12:21:18.308+00:00',
    updatedBy: 'system-seed',
    updatedWhen: '2026-08-28T12:21:18.308+00:00',
    deletedBy: null,
    deletedWhen: null,
    isDeleted: false,
    deletionReason: null,
    ...overrides
});

// The list is stubbed by the address it was reached at: what matters is which view the page
// let go to, not what that view renders.
const ListStub = () => {
    const location = useLocation();

    return <div data-testid="list">{`${location.pathname}${location.search}`}</div>;
};

const renderPage = (from?: string) =>
    render(
        <MemoryRouter
            initialEntries={[{
                pathname: `${settingsRoute}/${settingId}`,
                state: from == null ? null : { from }
            }]}>
            <Routes>
                <Route path={settingsRoute} element={<ListStub />} />
                <Route
                    path={`${settingsRoute}/:contentItemSettingId`}
                    element={<ContentItemSettingDetailPage />} />
            </Routes>
        </MemoryRouter>);

describe('ContentItemSettingDetailPage', () => {
    beforeEach(() => {
        setting = createSetting();
        isLoadingSetting = false;
        isErrorLoadingSetting = false;
        saveOutcome = 'succeeds';
        saved.mockReset();
    });

    describe('the way back', () => {
        it('should return to the view the page was opened from', async () => {
            // given
            renderPage(filteredRoute);

            // when
            await userEvent.click(
                screen.getByRole('button', { name: /Back to Content Item Settings/ }));

            // then: the filters, the scope and the page the administrator was working through
            expect(screen.getByTestId('list')).toHaveTextContent(filteredRoute);
        });

        it('should fall back to the bare list when there is no origin to honour', async () => {
            // given: reached directly — a pasted link, a refresh
            renderPage();

            // when
            await userEvent.click(
                screen.getByRole('button', { name: /Back to Content Item Settings/ }));

            // then
            expect(screen.getByTestId('list')).toHaveTextContent(settingsRoute);
        });

        it('should offer the way back even when the row could not be read', async () => {
            // given
            setting = null;
            isErrorLoadingSetting = true;
            renderPage(filteredRoute);

            // when
            await userEvent.click(
                screen.getByRole('button', { name: 'Back to Content Item Settings' }));

            // then
            expect(screen.getByTestId('list')).toHaveTextContent(filteredRoute);
        });
    });

    describe('saving', () => {
        it('should leave the same way Back does once the save goes through', async () => {
            // given
            renderPage(filteredRoute);

            await userEvent.clear(screen.getByLabelText('Sort order'));
            await userEvent.type(screen.getByLabelText('Sort order'), '7');

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Save settings' }));

            // then: what was typed was written, and the administrator is back where they were
            await waitFor(() =>
                expect(saved).toHaveBeenCalledWith(
                    expect.objectContaining({ id: settingId, sortOrder: 7 })));

            expect(screen.getByTestId('list')).toHaveTextContent(filteredRoute);
        });

        it('should return to the bare list when the page was reached directly', async () => {
            // given
            renderPage();

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Save settings' }));

            // then
            await waitFor(() => expect(screen.getByTestId('list')).toBeInTheDocument());
            expect(screen.getByTestId('list')).toHaveTextContent(settingsRoute);
        });

        it('should hold the reader on the form when the save is refused', async () => {
            // given
            saveOutcome = 'refuses';
            renderPage(filteredRoute);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Save settings' }));

            // then: a refusal is the one outcome with something left to say here
            expect(await screen.findByRole('alert')).toBeInTheDocument();
            expect(screen.queryByTestId('list')).not.toBeInTheDocument();
        });
    });
});
