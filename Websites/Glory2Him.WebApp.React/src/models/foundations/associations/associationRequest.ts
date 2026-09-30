import { EntityType } from '../approvalSettings/approvalSetting';

// The two endpoints of an association and nothing else (§ARC16.8.1): the server derives every
// other field. The types cross the wire as their numbers — the host registers no
// JsonStringEnumConverter.
export interface AssociationRequest {
    entityAType: EntityType;
    entityAKeyId: string;
    entityBType: EntityType;
    entityBKeyId: string;
}
