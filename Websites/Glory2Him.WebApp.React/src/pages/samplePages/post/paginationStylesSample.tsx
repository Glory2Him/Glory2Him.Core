import { useState } from 'react';
import { Card } from '../../../components/coreUI/card';
import { HeroBanner } from '../../../components/coreUI/heroBanner';
import { Pagination } from '../../../components/coreUI/pagination';
import { BreadcrumbItem } from '../../../models/coreUI/breadcrumbItem';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';

// Blogzine pagination-styles.html: every pagination variant side by side. These controls are
// live — clicking a page actually moves them.

const crumbs: ReadonlyArray<BreadcrumbItem> = [
    { title: 'Pagination styles', isActive: true },
];

export const PaginationStylesSample = () => {
    useDocumentTitle('Pagination Styles — Sample — Glory 2 Him');

    const [numberedPage, setNumberedPage] = useState(1);
    const [roundedPage, setRoundedPage] = useState(2);
    const [prevNextPage, setPrevNextPage] = useState(1);
    const [alignedPage, setAlignedPage] = useState(1);

    return (
        <SampleShell title="Pagination Styles" sourceFile="pagination-styles.html">
            <HeroBanner title="Pagination styles" crumbs={crumbs} />

            <section className="py-5">
                <div className="container">
                    <div className="row g-4">
                        <div className="col-lg-6">
                            <Card cssClass="border h-100" headerContent="Numbered">
                                <p className="text-body-secondary small">
                                    Square links with chevrons either side. Currently on page{' '}
                                    <strong>{numberedPage}</strong>.
                                </p>
                                <Pagination
                                    currentPage={numberedPage}
                                    onPageChange={setNumberedPage}
                                    totalPages={5} />
                            </Card>
                        </div>

                        <div className="col-lg-6">
                            <Card cssClass="border h-100" headerContent="Rounded">
                                <p className="text-body-secondary small">
                                    The same control with pill-shaped links. Currently on page{' '}
                                    <strong>{roundedPage}</strong>.
                                </p>
                                <Pagination
                                    currentPage={roundedPage}
                                    onPageChange={setRoundedPage}
                                    totalPages={5}
                                    variant="Rounded" />
                            </Card>
                        </div>

                        <div className="col-lg-6">
                            <Card cssClass="border h-100" headerContent="Previous / next only">
                                <p className="text-body-secondary small">
                                    No page numbers — useful for long article series. Currently on page{' '}
                                    <strong>{prevNextPage}</strong>.
                                </p>
                                <Pagination
                                    currentPage={prevNextPage}
                                    onPageChange={setPrevNextPage}
                                    totalPages={5}
                                    variant="PrevNext" />
                            </Card>
                        </div>

                        <div className="col-lg-6">
                            <Card cssClass="border h-100" headerContent="Left aligned">
                                <p className="text-body-secondary small">
                                    Centring turned off so the control sits with the content.
                                </p>
                                <Pagination
                                    currentPage={alignedPage}
                                    onPageChange={setAlignedPage}
                                    totalPages={3}
                                    alignment={false} />
                            </Card>
                        </div>
                    </div>
                </div>
            </section>
        </SampleShell>
    );
};
