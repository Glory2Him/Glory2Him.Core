import { ApprovalStatus } from '../../../../models/components/associations/associationItem';
import { DemoRadioOption } from './componentDoc';

// THE HOST'S APPROVAL STATUS an association demo hangs off — the post the panel would sit beside
// on a real page. The first choice hands the panel nothing at all, which is what every page does
// until its own task hands the post's status.
export const hostApprovalStatusNotGivenKey = 'not-given';

export const hostApprovalStatusOptions: ReadonlyArray<DemoRadioOption> = [
    { key: hostApprovalStatusNotGivenKey, label: 'not given (the default)' },
    { key: String(ApprovalStatus.Draft), label: 'Draft' },
    { key: String(ApprovalStatus.Submitted), label: 'Submitted' },
    { key: String(ApprovalStatus.Approved), label: 'Approved' },
    { key: String(ApprovalStatus.Rejected), label: 'Rejected' }
];
