import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import ApprovalSettingBroker from '../../brokers/apiBroker.approvalSettings';
import { ApprovalSetting } from '../../models/foundations/approvalSettings/approvalSetting';

// The §8.4 policy rows behind the approval evaluation. Every write invalidates the whole slice
// AND the approval reads that resolve against it: changing how many approvals a content type
// needs changes the verdict of every round already on screen, and a panel still holding the old
// one would offer an approve the server is about to refuse.
const approvalSettingStaleTime = 60 * 1000;

const invalidateApprovalSettings = (
    queryClient: ReturnType<typeof useQueryClient>,
    approvalSettingId?: string) => {
    queryClient.invalidateQueries({ queryKey: ['ApprovalSettingsGetAll'] });

    if (approvalSettingId != null) {
        queryClient.invalidateQueries({
            queryKey: ['ApprovalSettingsGetById', approvalSettingId]
        });
    }

    // THE POLICY IS WHAT THE VERDICT IS COMPUTED FROM, so a change to it reaches every round.
    queryClient.invalidateQueries({ queryKey: ['ApprovalVerdict'] });
};

export const approvalSettingService = {
    useGetApprovalSettings: () => {
        const approvalSettingBroker = new ApprovalSettingBroker();

        return useQuery<ApprovalSetting[]>({
            queryKey: ['ApprovalSettingsGetAll'],
            queryFn: async () => await approvalSettingBroker.GetApprovalSettingsAsync(),
            staleTime: approvalSettingStaleTime
        });
    },

    useGetApprovalSettingById: (approvalSettingId: string, enabled = true) => {
        const approvalSettingBroker = new ApprovalSettingBroker();

        return useQuery<ApprovalSetting>({
            queryKey: ['ApprovalSettingsGetById', approvalSettingId],
            queryFn: async () =>
                await approvalSettingBroker.GetApprovalSettingByIdAsync(approvalSettingId),
            enabled: enabled && approvalSettingId.length > 0,
            staleTime: approvalSettingStaleTime
        });
    },

    // suppressGlobalErrorToast on every write: the API is the authority on what a policy row may
    // carry — a duplicate scope is a 409, a content type on the wrong entity type a 424 — and the
    // page shows that message itself rather than letting the generic toast talk over it.
    useAddApprovalSetting: () => {
        const approvalSettingBroker = new ApprovalSettingBroker();
        const queryClient = useQueryClient();

        return useMutation({
            meta: { suppressGlobalErrorToast: true },

            mutationFn: async (approvalSetting: ApprovalSetting) =>
                await approvalSettingBroker.AddApprovalSettingAsync(approvalSetting),

            onSuccess: (_, approvalSetting) =>
                invalidateApprovalSettings(queryClient, approvalSetting.id)
        });
    },

    useUpdateApprovalSetting: () => {
        const approvalSettingBroker = new ApprovalSettingBroker();
        const queryClient = useQueryClient();

        return useMutation({
            meta: { suppressGlobalErrorToast: true },

            mutationFn: async (approvalSetting: ApprovalSetting) =>
                await approvalSettingBroker.UpdateApprovalSettingAsync(approvalSetting),

            onSuccess: (_, approvalSetting) =>
                invalidateApprovalSettings(queryClient, approvalSetting.id)
        });
    },

    useRemoveApprovalSetting: () => {
        const approvalSettingBroker = new ApprovalSettingBroker();
        const queryClient = useQueryClient();

        return useMutation({
            meta: { suppressGlobalErrorToast: true },

            mutationFn: async (
                request: { approvalSettingId: string; deletionReason?: string }) =>
                await approvalSettingBroker.RemoveApprovalSettingByIdAsync(
                    request.approvalSettingId, request.deletionReason),

            onSuccess: (_, request) =>
                invalidateApprovalSettings(queryClient, request.approvalSettingId)
        });
    }
};
