import { useEffect, useState } from 'react';
import { Breadcrumb } from '../components/coreUI/breadcrumb';
import { Button } from '../components/coreUI/button';
import { Card } from '../components/coreUI/card';
import { ConfirmDialog } from '../components/coreUI/confirmDialog';
import { FormSelect } from '../components/coreUI/formSelect';
import { FormSwitch } from '../components/coreUI/formSwitch';
import { FormText } from '../components/coreUI/formText';
import { Modal } from '../components/coreUI/modal';
import { PageHeader } from '../components/coreUI/pageHeader';
import { StatTile } from '../components/coreUI/statTile';
import { BreadcrumbItem } from '../models/coreUI/breadcrumbItem';
import { SelectOption } from '../models/coreUI/selectOption';

// A single, consolidated component/style guide — the Glory 2 Him equivalent of the Blogzine
// docs pages, showing the reusable CoreUI components in one place. Doubles as a smoke-test
// page that every component renders.
const demoCrumbs: ReadonlyArray<BreadcrumbItem> = [
    { title: 'Library', href: '#' },
    { title: 'Components', href: '#', isActive: true },
];

const demoOptions: ReadonlyArray<SelectOption> = [
    { value: '1', text: 'Option one' },
    { value: '2', text: 'Option two' },
    { value: '3', text: 'Option three' },
];

export function StyleGuide() {
    const [textValue, setTextValue] = useState('Hello');
    const [selectValue, setSelectValue] = useState('2');
    const [switchValue, setSwitchValue] = useState(true);
    const [modalVisible, setModalVisible] = useState(false);
    const [confirmVisible, setConfirmVisible] = useState(false);

    useEffect(() => {
        document.title = 'Style guide — Glory 2 Him';
    }, []);

    return (
        <>
            <PageHeader title="Style guide" />

            <section className="pt-4 pb-5">
                <div className="container">

                    <h3 className="mb-3">Buttons</h3>
                    <div className="d-flex flex-wrap gap-2 mb-5">
                        <Button color="primary">Primary</Button>
                        <Button color="success">Success</Button>
                        <Button color="danger">Danger</Button>
                        <Button color="warning">Warning</Button>
                        <Button color="outline-primary">Outline</Button>
                        <Button color="primary" disabled>Disabled</Button>
                    </div>

                    <h3 className="mb-3">Stat tiles</h3>
                    <div className="row g-4 mb-5">
                        <div className="col-sm-6 col-lg-3"><StatTile variant="Green" icon="bi-check-circle-fill" value="128" label="Green" /></div>
                        <div className="col-sm-6 col-lg-3"><StatTile variant="Amber" icon="bi-exclamation-triangle-fill" value="42" label="Amber" /></div>
                        <div className="col-sm-6 col-lg-3"><StatTile variant="Red" icon="bi-x-circle-fill" value="7" label="Red" /></div>
                        <div className="col-sm-6 col-lg-3"><StatTile variant="Na" icon="bi-info-circle-fill" value="—" label="Neutral" /></div>
                    </div>

                    <h3 className="mb-3">Cards</h3>
                    <div className="row g-4 mb-5">
                        <div className="col-md-6">
                            <Card title="Card with header">
                                <p className="mb-0">A simple card using the reusable CoreUI Card component.</p>
                            </Card>
                        </div>
                        <div className="col-md-6">
                            <Card>
                                <p className="mb-0">A card with no header, body only.</p>
                            </Card>
                        </div>
                    </div>

                    <h3 className="mb-3">Breadcrumbs</h3>
                    <div className="mb-5">
                        <Breadcrumb items={demoCrumbs} />
                    </div>

                    <h3 className="mb-3">Form controls</h3>
                    <div className="row mb-5">
                        <div className="col-md-6">
                            <FormText label="Text field" value={textValue} onValueChange={setTextValue} />
                            <FormSelect label="Select" value={selectValue} options={demoOptions} onValueChange={setSelectValue} />
                            <FormSwitch label="Switch" value={switchValue} onValueChange={setSwitchValue} />
                        </div>
                        <div className="col-md-6">
                            <p className="small text-body-secondary">Live values</p>
                            <ul className="small">
                                <li>Text: <code>{textValue}</code></li>
                                <li>Select: <code>{selectValue}</code></li>
                                <li>Switch: <code>{String(switchValue)}</code></li>
                            </ul>
                        </div>
                    </div>

                    <h3 className="mb-3">Overlays</h3>
                    <div className="d-flex gap-2 mb-5">
                        <Button color="primary" onClick={() => setModalVisible(true)}>Open modal</Button>
                        <Button color="danger" onClick={() => setConfirmVisible(true)}>Open confirm dialog</Button>
                    </div>

                    <Modal
                        title="Example modal"
                        visible={modalVisible}
                        onClose={() => setModalVisible(false)}
                        footerContent={
                            <Button color="secondary" onClick={() => setModalVisible(false)}>Close</Button>
                        }>
                        <p className="mb-0">This is the reusable CoreUI Modal, restyled to the Blogzine theme.</p>
                    </Modal>

                    <ConfirmDialog
                        visible={confirmVisible}
                        title="Are you sure?"
                        message="This is the reusable ConfirmDialog component."
                        onConfirm={() => setConfirmVisible(false)}
                        onCancel={() => setConfirmVisible(false)} />
                </div>
            </section>
        </>
    );
}
