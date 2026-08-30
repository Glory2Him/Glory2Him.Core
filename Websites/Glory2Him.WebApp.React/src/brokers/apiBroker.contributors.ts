import { ContributorSummary } from '../models/foundations/contributors/contributorSummary';
import ApiBroker from './apiBroker';

class ContributorBroker {
    relativeContributorsUrl = '/api/contributors';
    private apiBroker: ApiBroker = new ApiBroker();

    // The byline identity behind a ContentItem's CreatedBy. Anonymous, so a signed-out reader
    // gets a name under the contribution they are reading; a 404 means no such account, which the
    // caller renders as no byline rather than as an error.
    async GetContributorByIdAsync(userId: string): Promise<ContributorSummary> {
        const url = `${this.relativeContributorsUrl}/${userId}`;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as ContributorSummary;
    }
}

export default ContributorBroker;
