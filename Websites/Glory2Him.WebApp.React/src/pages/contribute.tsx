import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { toastError } from '../brokers/toastBroker.error';
import { toastSuccess } from '../brokers/toastBroker.success';
import { ContentItemPanel } from '../components/contentItems/contentItemPanel';
import { Spinner } from '../components/coreUI/spinner';

import {
    ContentItemFormItem,
    ContentItemValidationIssues
} from '../models/components/contentItems/contentItemFormItem';

import { contentItemService } from '../services/foundations/contentItemService';
import { contentItemSettingService } from '../services/foundations/contentItemSettingService';
import { toContentItemApiFailure } from '../services/views/contentItems/toContentItemApiFailure';
import { toContentItemAddRequest } from '../services/views/contentItems/toContentItemAddRequest';
import { useDocumentTitle } from './useDocumentTitle';

// The contribution page. The form itself is ContentItemPanel on its add face — the bespoke
// markup that used to live here (its own field layout, its own type picker, a dead submit button)
// is gone, and with it the tag and bible-reference boxes: those belong to the association panels,
// which need an item to associate to and therefore cannot render before one exists (design
// §20.6.2). They render beside the item on its own page instead.
//
// THE PAGE OWNS PERSISTENCE. The panel raises onAdded and nothing more; everything below —
// the POST, the redirect, the notification and the validation readback — is this page's work.
const contributeFailureText =
    'Your contribution could not be submitted. Please try again.';

// Design §3.4.2 rule 6's own wording. It is the ONE thing said on a successful submission,
// whether or not a row was written, so it must not hint at either.
const contributeThanksText =
    'Thank you for your submission. It will be reviewed before publishing.';

export function Contribute() {
    useDocumentTitle('Share what He has done — Glory 2 Him');
    const navigate = useNavigate();

    const { data: contentTypeSettings, isLoading, isError } =
        contentItemSettingService.useGetAvailableForContribution();

    const addContentItem = contentItemService.useAddContentItem();

    const [validationIssues, setValidationIssues] =
        useState<ContentItemValidationIssues | undefined>();

    // The API is the authority on what a content item must carry, so nothing is pre-judged here:
    // the submission goes, and whatever comes back marks up the form the reader is looking at.
    const addContentItemAsync = async (formItem: ContentItemFormItem) => {
        setValidationIssues(undefined);

        try {
            await addContentItem.mutateAsync(toContentItemAddRequest(formItem));

            // THE RESPONSE BODY IS DELIBERATELY UNREAD, and that is the whole of design
            // §3.4.2 rule 6 on this side. A contribution whose content has already been
            // submitted is accepted quietly: the API answers 201 with an item shaped exactly
            // like a created one, but no row was written, so following its id would land the
            // contributor on a 404 — and a 404 tells them their content is a duplicate just as
            // plainly as the error message this replaced (#392, #412). The page therefore
            // thanks them and lands them on their own posts, which is the same journey for a
            // genuine submission and for a duplicate.
            toastSuccess(contributeThanksText);

            // The contributor's OWN surface, not the public one: a fresh contribution is a
            // Draft or awaiting review, depending on what the "Submit as" row was left on, and
            // /myposts is where either is theirs to read.
            navigate('/myposts');
        } catch (error) {
            const failure = toContentItemApiFailure(error, contributeFailureText);

            setValidationIssues(failure.validationIssues);
            toastError(failure.message);
        }
    };

    return (
        <section className="pt-4 pb-5">
            <div className="container">
                <div className="row justify-content-center">
                    <div className="col-xl-9">
                        <div className="text-center mb-4">
                            <i
                                className="bi bi-pencil-square text-primary display-5"
                                aria-hidden="true"></i>

                            <h1 className="mt-2 mb-2">Share what He has done</h1>
                            <p className="lead mb-0">
                                A story, a testimony, or a verse that carried you through — if it
                                might encourage someone else, we would love to read it. Submissions
                                are reviewed before publishing.
                            </p>
                        </div>

                        {isLoading ? (
                            <div className="text-center py-5"><Spinner /></div>
                        ) : isError ? (
                            <div className="alert alert-danger" role="alert">
                                We could not load the contribution types right now. Please try again later.
                            </div>
                        ) : (
                            <div className="card card-body border p-4 p-lg-5">
                                <ContentItemPanel
                                    ariaLabel="Share what He has done"
                                    contentItemSettingCollection={contentTypeSettings ?? []}
                                    validationIssues={validationIssues}
                                    isSubmitting={addContentItem.isPending}
                                    onAdded={addContentItemAsync}
                                    onCancelled={() => navigate('/')} />
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </section>
    );
}
