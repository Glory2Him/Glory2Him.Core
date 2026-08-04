import { AuthCard } from '../../../components/coreUI/authCard';
import { Button } from '../../../components/coreUI/button';
import { FormSwitch } from '../../../components/coreUI/formSwitch';
import { FormText } from '../../../components/coreUI/formText';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';

// Blogzine signup.html. A layout demo only — the real registration lives at /Account/Register.
export const SignupSample = () => {
    useDocumentTitle('Signup — Sample — Glory 2 Him');

    return (
        <SampleShell title="Signup" sourceFile="signup.html">
            <AuthCard
                title="Join the journal"
                subtitle="Create an account to save the stories that encourage you."
                footerPrompt="Already have an account?"
                footerLinkText="Sign in"
                footerHref="/SamplePages/Pages/Signin">
                <div className="row">
                    <div className="col-md-6">
                        <FormText label="First name" placeholder="Jane" />
                    </div>
                    <div className="col-md-6">
                        <FormText label="Surname" placeholder="Doe" />
                    </div>
                </div>

                <FormText label="Email address" placeholder="jane@example.com" />
                <FormText label="Password" placeholder="••••••••" />
                <FormText label="Confirm password" placeholder="••••••••" />

                <div className="mb-3">
                    <FormSwitch label="I agree to the terms and privacy policy" value={false} />
                </div>

                <div className="d-grid">
                    <Button color="primary">Create account</Button>
                </div>
            </AuthCard>
        </SampleShell>
    );
};
