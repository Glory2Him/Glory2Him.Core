import ApiBroker from './apiBroker';

import {
    AIReviewerAssignment,
    AIReviewerStatus,
    EntityTypeName
} from '../models/foundations/approvals/approval';

// api/AIReviewers — BEREAN'S OWN RESOURCE (design §8.6.2), and its own broker for the same
// reason the server gave it its own controller: an AI assignment is not part of the approval
// round's contract. The round answers what may happen to an approval and who is judging it;
// this answers whether Berean is offered, whether it has been asked and how far it has got.
// Bolting the two together gave one broker two subjects, exactly as it gave one controller two.
//
// ONE RESOURCE, THREE VERBS, and no reviewer id anywhere in the address. A person's review
// request is named by the account it was addressed to; Berean is not a role-bearing identity and
// has no account to name, so the ENTITY is the whole key and the verb is what says whether to
// read, assign or withdraw. That is why there is no id segment here and no body on the writes.
class AIReviewerBroker {
    // Spelled the way the controller reads rather than folded flat like the older siblings in
    // this folder: routing is case-insensitive, so this is a matter of what a network log says.
    relativeAIReviewersUrl = '/api/aiReviewers';
    private apiBroker: ApiBroker = new ApiBroker();

    // BEREAN'S STATUS — one small read answering both "should the picker offer it" and "what's
    // it doing right now", keyed by the entity like the candidates and requests on the round.
    async GetAIReviewerStatusAsync(
        entityType: EntityTypeName,
        entityId: string): Promise<AIReviewerStatus> {
        const url = `${this.relativeAIReviewersUrl}/${entityType}/${entityId}`;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as AIReviewerStatus;
    }

    // ASSIGN — or RE-REQUEST. The endpoint is an upsert: no live row creates one, a completed
    // one resets to pending, a still-pending one is a no-op that just returns the standing row.
    async PostAIReviewerAsync(
        entityType: EntityTypeName,
        entityId: string): Promise<AIReviewerAssignment> {
        const url = `${this.relativeAIReviewersUrl}/${entityType}/${entityId}`;
        const result = await this.apiBroker.PostAsync(url, {});

        return result.data as AIReviewerAssignment;
    }

    // WITHDRAW. Unconditional — re-request now covers "ask again after completion", so there is
    // no answered-invitation refusal to keep out of reach here the way there is for a person's.
    // Nothing standing is 204 (no body) rather than 200, so the result is nullable — unused by
    // the hook either way, since Berean's own status read is what the UI repaints from.
    //
    // AN EMPTY BODY IS NOTHING STANDING, and it is checked for as an empty STRING rather than as
    // undefined: axios materialises a 204 by handing the default transform an empty response
    // body, which it cannot parse as JSON and gives back verbatim — so `?? null` alone would
    // return '' and quietly contradict the nullable this promises.
    async DeleteAIReviewerAsync(
        entityType: EntityTypeName,
        entityId: string): Promise<AIReviewerAssignment | null> {
        const url = `${this.relativeAIReviewersUrl}/${entityType}/${entityId}`;
        const result = await this.apiBroker.DeleteAsync(url);
        const assignment = result.data as AIReviewerAssignment | '' | null | undefined;

        return assignment == null || assignment === '' ? null : assignment;
    }
}

export default AIReviewerBroker;
