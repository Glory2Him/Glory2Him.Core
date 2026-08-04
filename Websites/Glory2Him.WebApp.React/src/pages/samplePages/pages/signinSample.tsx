import { Link } from 'react-router-dom';
import { AuthCard } from '../../../components/coreUI/authCard';
import { Button } from '../../../components/coreUI/button';
import { FormSwitch } from '../../../components/coreUI/formSwitch';
import { FormText } from '../../../components/coreUI/formText';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';

// Blogzine signin.html. A layout demo only — the real sign-in lives at /Account/Login.
export const SigninSample = () => {
    useDocumentTitle('Signin — Sample — Glory 2 Him');

    return (
        <SampleShell title="Signin" sourceFile="signin.html">
            <AuthCard
                title="Welcome back"
                subtitle="Sign in to pick up where you left off."
                footerPrompt="New here?"
                footerLinkText="Create an account"
                footerHref="/SamplePages/Pages/Signup">
                <FormText label="Email address" placeholder="jane@example.com" />
                <FormText label="Password" placeholder="••••••••" />

                <div className="d-flex justify-content-between align-items-center mb-3">
                    <FormSwitch label="Keep me signed in" value={true} />
                    <Link to="/Account/ForgotPassword" className="btn-link small">Forgot password?</Link>
                </div>

                <div className="d-grid">
                    <Button color="primary">Sign me in</Button>
                </div>
            </AuthCard>
        </SampleShell>
    );
};
