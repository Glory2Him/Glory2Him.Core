import { ReactNode } from 'react';
import { AuthContextOverride } from '../../../../components/securitys/authProvider';
import { DemoRadioGroup } from './componentDoc';

// THE SECURITY CONTEXT a playground demo runs under. The family's components are pure
// presentation — every ownership and role gate decides rendering only, and the server
// re-decides each write — so a reference page may honestly step its demo into any viewer
// and show what that person would be offered.

export const demoViewerId = 'demo-viewer';
export const demoOtherSubmitterId = 'somebody-else';

export type SecurityContextOption = {
    key: string;
    label: string;
    roles: ReadonlyArray<string>;
    isOwner: boolean;
};

export const securityContextOptions: ReadonlyArray<SecurityContextOption> = [
    { key: 'submitter', label: 'I am the submitter (owner)', roles: [], isOwner: true },
    { key: 'reviewer', label: 'I am a reviewer (not owner)', roles: ['Reviewers'], isOwner: false },
    {
        key: 'publisher-owner',
        label: 'I am a publisher (also owner)',
        roles: ['Publishers'],
        isOwner: true
    },
    { key: 'publisher', label: 'I am a publisher (not owner)', roles: ['Publishers'], isOwner: false },
    {
        key: 'administrator-owner',
        label: 'I am an administrator (also owner)',
        roles: ['Administrators'],
        isOwner: true
    },
    {
        key: 'administrator',
        label: 'I am an administrator (not owner)',
        roles: ['Administrators'],
        isOwner: false
    }
];

// Who the demo item says submitted it, given who the reader is pretending to be — the other
// half of every ownership gate.
export const demoSubmitterIdFor = (option: SecurityContextOption): string =>
    option.isOwner ? demoViewerId : demoOtherSubmitterId;

export interface SecurityContextSectionProps {
    selected: SecurityContextOption;
    onChange: (option: SecurityContextOption) => void;

    // Appended after the six baseline personas, for a demo whose component recognizes further
    // roles the shared list has no reason to carry for every consumer — an entity-scoped
    // moderation tier, say. Empty by default, so every other doc page is unaffected.
    extraOptions?: ReadonlyArray<SecurityContextOption>;
}

// The radio board — the shared DemoRadioGroup wearing the people a demo can be: the six
// baseline personas, plus whichever extra ones this particular component's page adds.
export function SecurityContextSection({
    selected,
    onChange,
    extraOptions = []
}: SecurityContextSectionProps) {
    const options = [...securityContextOptions, ...extraOptions];

    return (
        <DemoRadioGroup
            title="Security context"
            name="demo-security-context"
            options={options}
            selectedKey={selected.key}
            onChange={(key) => {
                const option = options.find((candidate) => candidate.key === key);

                if (option != null) {
                    onChange(option);
                }
            }} />
    );
}

export interface DemoSecurityContextProps {
    option: SecurityContextOption;
    children: ReactNode;
}

// The demo subtree, viewed as the chosen person.
export function DemoSecurityContext({ option, children }: DemoSecurityContextProps) {
    return (
        <AuthContextOverride
            userId={demoViewerId}
            displayName="Demo Viewer"
            roles={option.roles}>
            {children}
        </AuthContextOverride>
    );
}
