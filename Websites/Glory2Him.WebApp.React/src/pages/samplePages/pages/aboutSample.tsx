import { Avatar } from '../../../components/coreUI/avatar';
import { Card } from '../../../components/coreUI/card';
import { HeroBanner } from '../../../components/coreUI/heroBanner';
import { Newsletter } from '../../../components/coreUI/newsletter';
import { StatTile } from '../../../components/coreUI/statTile';
import { BreadcrumbItem } from '../../../models/coreUI/breadcrumbItem';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';
import { useSamplePosts } from '../shared/useSamplePosts';

// Blogzine about-us.html: hero, mission copy, a counter row and the team.

const crumbs: ReadonlyArray<BreadcrumbItem> = [
    { title: 'About', isActive: true },
];

const teamMembers: ReadonlyArray<string> =
    ['Joan Wallace', 'Lori Stevens', 'Dennis Barrett', 'Carolyn Ortiz'];

export const AboutSample = () => {
    useDocumentTitle('About — Sample — Glory 2 Him');

    const { posts } = useSamplePosts();

    return (
        <SampleShell title="About" sourceFile="about-us.html">
            <HeroBanner title="About us" crumbs={crumbs} />

            <section className="py-5">
                <div className="container">
                    <div className="row justify-content-center text-center mb-5">
                        <div className="col-lg-8">
                            <h2 className="mb-3">Sharing the good news of Jesus Christ</h2>
                            <p className="lead text-body-secondary mb-0">
                                Glory 2 Him is a place to read, reflect, and be encouraged — stories that
                                point all glory back to Him.
                            </p>
                        </div>
                    </div>

                    <div className="row g-4 mb-5">
                        <div className="col-sm-6 col-lg-3">
                            <StatTile
                                variant="Green"
                                value={String(posts.length)}
                                label="Stories"
                                icon="bi-file-earmark-text" />
                        </div>
                        <div className="col-sm-6 col-lg-3">
                            <StatTile variant="Amber" value="12" label="Contributors" icon="bi-people" />
                        </div>
                        <div className="col-sm-6 col-lg-3">
                            <StatTile variant="Na" value="48" label="Countries reached" icon="bi-globe" />
                        </div>
                        <div className="col-sm-6 col-lg-3">
                            <StatTile variant="Red" value="7" label="Years running" icon="bi-calendar-heart" />
                        </div>
                    </div>

                    <h2 className="h4 mb-4">Our team</h2>

                    <div className="row g-4">
                        {teamMembers.map((member) => (
                            <div className="col-sm-6 col-lg-3" key={member}>
                                <Card cssClass="border text-center h-100">
                                    <Avatar name={member} sizePx={80} sizeCssClass="mx-auto mb-3" />
                                    <h6 className="mb-1">{member}</h6>
                                    <p className="small text-body-secondary mb-0">Contributor</p>
                                </Card>
                            </div>
                        ))}
                    </div>
                </div>
            </section>

            <section className="pb-5">
                <div className="container">
                    <Newsletter />
                </div>
            </section>
        </SampleShell>
    );
};
