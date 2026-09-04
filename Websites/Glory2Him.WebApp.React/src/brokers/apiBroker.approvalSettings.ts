import ApiBroker from './apiBroker';
import {
    ApprovalSetting,
    ApprovalSettingAddRequest
} from '../models/foundations/approvalSettings/approvalSetting';

// api/ApprovalSettings — the §8.4 policy rows the approval evaluation resolves against. Six
// endpoints; this broker uses five of them (the hard delete is deliberately not offered to a UI).
class ApprovalSettingBroker {
    relativeApprovalSettingsUrl = '/api/approvalsettings';
    private apiBroker: ApiBroker = new ApiBroker();

    // The whole live set, ordered so the entity-type defaults sit above the content-type rows
    // that override them. Soft-deleted rows are filtered server-side rather than dropped here —
    // a closed setting is not a policy and should not travel.
    //
    // No paging: the set is bounded by entity types times content types, which is small enough
    // to read whole and page in the browser. That is why there is no query round-tripper here
    // and why the list uses the shared DataTable rather than a server-side pager.
    async GetApprovalSettingsAsync(): Promise<ApprovalSetting[]> {
        // Built by hand rather than through URLSearchParams, which writes a space as + — legal,
        // and decoded correctly on the way in, but it makes every logged query harder to read
        // than it needs to be, and the sibling settings broker already spells its filters out.
        const filter = encodeURIComponent('isDeleted eq false');
        const orderBy = 'entityType,contentType';

        const url =
            `${this.relativeApprovalSettingsUrl}?$filter=${filter}&$orderby=${orderBy}`;

        const result = await this.apiBroker.GetAsync(url);

        return result.data as ApprovalSetting[];
    }

    async GetApprovalSettingByIdAsync(approvalSettingId: string): Promise<ApprovalSetting> {
        const url = `${this.relativeApprovalSettingsUrl}/${approvalSettingId}`;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as ApprovalSetting;
    }

    // The id travels in the BODY, minted by the caller — the service refuses an empty Guid and
    // never generates one of its own. The audit fields do not travel: the server stamps them,
    // and an empty one is refused in model binding (see ApprovalSettingAddRequest).
    async AddApprovalSettingAsync(
        approvalSetting: ApprovalSettingAddRequest): Promise<ApprovalSetting> {
        const result = await this.apiBroker.PostAsync(
            this.relativeApprovalSettingsUrl, approvalSetting);

        return result.data as ApprovalSetting;
    }

    // PUT to the COLLECTION url with no id segment: the exposer routes on the body's Id, and the
    // whole entity goes — audit fields included, because the foundation compares CreatedBy and
    // CreatedWhen against storage before it will accept the write.
    async UpdateApprovalSettingAsync(approvalSetting: ApprovalSetting): Promise<ApprovalSetting> {
        const result = await this.apiBroker.PutAsync(
            this.relativeApprovalSettingsUrl, approvalSetting);

        return result.data as ApprovalSetting;
    }

    // The SOFT delete, which keeps the row and its audit trail. The /Hard sibling is not exposed
    // here: erasing a policy row is not something a screen should offer.
    async RemoveApprovalSettingByIdAsync(
        approvalSettingId: string,
        deletionReason?: string): Promise<ApprovalSetting> {
        const reason = (deletionReason ?? '').trim();

        const url = reason.length > 0
            ? `${this.relativeApprovalSettingsUrl}/${approvalSettingId}`
                + `?deletionReason=${encodeURIComponent(reason)}`
            : `${this.relativeApprovalSettingsUrl}/${approvalSettingId}`;

        const result = await this.apiBroker.DeleteAsync(url);

        return result.data as ApprovalSetting;
    }
}

export default ApprovalSettingBroker;
