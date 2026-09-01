import { ApprovalStatus } from '../../components/associations/associationItem';

// Wire shape of api/Reactions — the reaction VOCABULARY (a name and its emoji), camelCased by the
// host's default System.Text.Json policy. Only what the choices surface reads is typed; the audit
// members ride along untyped exactly as the other wire models leave what they do not use.
export type Reaction = {
    id: string;
    name: string;
    unicodeEmoji: string;
    isPublished: boolean;
    approvalStatus: ApprovalStatus;
    isDeleted: boolean;
};
