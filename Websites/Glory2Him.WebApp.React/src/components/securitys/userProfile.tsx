import { ReactElement, useState } from "react"
import { Button, Card, ListGroup, Modal, NavDropdown } from "react-bootstrap";
import { useAuth } from './authProvider';

export const UserProfile = (): ReactElement => {
    const { user, userRoles } = useAuth();
    const [showModal, setShowModal] = useState(false);
    const closeModal = () => setShowModal(false);
    const openModal = () => setShowModal(true);

    return (
        <div>
            <Modal show={showModal} onHide={closeModal} size="lg" centered>
                <Modal.Header closeButton>
                    <Modal.Title>My Profile</Modal.Title>
                </Modal.Header>
                <Modal.Body>
                    <Card>
                        <Card.Body>
                            <ListGroup variant="flush">
                                {/*
                                    Two rows, not one. This used to read "Username / Email" and
                                    show whichever was set — wording from the days when the two
                                    were deliberately the same value. Design §18.3.1 rules that
                                    they never are: the username is public wherever the site names
                                    who submitted or reviewed something, and the email is not.
                                    Labelling them as interchangeable is what teaches somebody to
                                    type their address into the username box.
                                */}
                                <ListGroup.Item>
                                    <div className="d-flex justify-content-between align-items-center">
                                        <div className="fw-bold">Username</div>
                                        <div>{user?.userName}</div>
                                    </div>
                                </ListGroup.Item>
                                <ListGroup.Item>
                                    <div className="d-flex justify-content-between align-items-center">
                                        <div className="fw-bold">Email</div>
                                        <div>{user?.email}</div>
                                    </div>
                                </ListGroup.Item>
                                <ListGroup.Item>
                                    <div className="d-flex justify-content-between align-items-center">
                                        <div className="fw-bold">Name</div>
                                        <div>{user?.displayName}</div>
                                    </div>
                                </ListGroup.Item>
                                {userRoles.map((role, index) => (
                                    <ListGroup.Item key={index}>
                                        <div className="d-flex justify-content-between align-items-center">
                                            <div className="fw-bold">Role</div>
                                            <div>{role}</div>
                                        </div>
                                    </ListGroup.Item>
                                ))}
                            </ListGroup>
                        </Card.Body>
                    </Card>
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="danger" onClick={closeModal}>Close</Button>
                </Modal.Footer>
            </Modal>

            <NavDropdown.Item onClick={openModal}>My Profile</NavDropdown.Item>
        </div>
    );
};
