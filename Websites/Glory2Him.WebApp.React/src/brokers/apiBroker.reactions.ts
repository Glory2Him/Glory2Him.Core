import ApiBroker from './apiBroker';
import { Reaction } from '../models/foundations/reactions/reaction';

class ReactionBroker {
    relativeReactionsUrl = '/api/reactions';
    private apiBroker: ApiBroker = new ApiBroker();

    // The reaction VOCABULARY — the choices a reader may pick from, not anybody's given
    // reactions. Narrowed server-side to the approved, published rows through [EnableQuery]:
    // the read is [AllowAnonymous] and widens with the caller (an owner sees their own drafts),
    // and a draft reaction offered as a choice would let somebody react with something the
    // moderators have not accepted yet.
    async GetApprovedReactionsAsync(): Promise<Reaction[]> {
        const filter = "approvalStatus eq 'Approved' and isPublished eq true and isDeleted eq false";
        const url = `${this.relativeReactionsUrl}?$filter=${encodeURIComponent(filter)}`;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as Reaction[];
    }
}

export default ReactionBroker;
