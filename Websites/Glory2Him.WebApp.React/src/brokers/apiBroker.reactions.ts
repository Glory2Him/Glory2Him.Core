import ApiBroker from './apiBroker';
import { Reaction } from '../models/foundations/reactions/reaction';
import { ODataQuery } from '../models/foundations/oDataQueries/oDataQuery';

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

    async GetReactionsAsync(query: ODataQuery): Promise<Reaction[]> {
        const parameters = new URLSearchParams();

        if (query.filter !== undefined) {
            parameters.append('$filter', query.filter);
        }

        if (query.orderBy !== undefined) {
            parameters.append('$orderby', query.orderBy);
        }

        if (query.skip !== undefined) {
            parameters.append('$skip', String(query.skip));
        }

        if (query.top !== undefined) {
            parameters.append('$top', String(query.top));
        }

        const queryString = parameters.toString();
        const url = queryString ? `${this.relativeReactionsUrl}?${queryString}` : this.relativeReactionsUrl;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as Reaction[];
    }
}

export default ReactionBroker;
