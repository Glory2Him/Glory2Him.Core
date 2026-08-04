import { FormEvent, useState } from "react";
import { Alert, Button, Card, Form } from "react-bootstrap";
import { useNavigate, useSearchParams } from "react-router-dom";
import { accountService } from "../services/foundations/accountService";

export const Login = () => {
    const [userName, setUserName] = useState('');
    const [password, setPassword] = useState('');
    const [rememberMe, setRememberMe] = useState(false);
    const [failed, setFailed] = useState(false);
    const login = accountService.useLogin();
    const navigate = useNavigate();
    const [searchParams] = useSearchParams();

    const handleSubmit = (event: FormEvent) => {
        event.preventDefault();
        setFailed(false);

        login.mutate({ userName, password, rememberMe }, {
            onSuccess: () => {
                const returnUrl = searchParams.get('returnUrl') || '/';
                navigate(returnUrl);
            },
            onError: () => {
                setFailed(true);
            }
        });
    }

    return (
        <div className="container mt-5">
            <div className="row justify-content-center">
                <div className="col-md-6 col-lg-4">
                    <Card>
                        <Card.Body>
                            <h1 className="h4 mb-4 text-center">Sign in</h1>
                            {failed && (
                                <Alert variant="danger">
                                    Invalid username or password.
                                </Alert>
                            )}
                            <Form onSubmit={handleSubmit}>
                                <Form.Group className="mb-3" controlId="loginUserName">
                                    <Form.Label>Username</Form.Label>
                                    <Form.Control
                                        type="text"
                                        value={userName}
                                        autoComplete="username"
                                        onChange={(event) => setUserName(event.target.value)} />
                                </Form.Group>
                                <Form.Group className="mb-3" controlId="loginPassword">
                                    <Form.Label>Password</Form.Label>
                                    <Form.Control
                                        type="password"
                                        value={password}
                                        autoComplete="current-password"
                                        onChange={(event) => setPassword(event.target.value)} />
                                </Form.Group>
                                <Form.Group className="mb-3" controlId="loginRememberMe">
                                    <Form.Check
                                        type="checkbox"
                                        label="Remember me"
                                        checked={rememberMe}
                                        onChange={(event) => setRememberMe(event.target.checked)} />
                                </Form.Group>
                                <div className="d-grid">
                                    <Button type="submit" disabled={login.isPending}>
                                        {login.isPending ? 'Signing in…' : 'Sign in'}
                                    </Button>
                                </div>
                            </Form>
                        </Card.Body>
                    </Card>
                </div>
            </div>
        </div>
    );
}
