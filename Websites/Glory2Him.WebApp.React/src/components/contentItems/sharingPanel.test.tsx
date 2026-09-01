import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { SharingPanel } from './sharingPanel';

// The invitation to contribute. No router: the button raises an EVENT, and where it leads is the
// page's decision. The wide/narrow adaptation is a CSS container query, which jsdom cannot
// exercise — what is pinned here instead is the structure the query depends on: the icon inline
// in the heading (so a narrow title wraps beneath it) and the never-wrapping button text.
describe('SharingPanel', () => {
    it('should render its defaults as the design wrote them', () => {
        // when
        render(<SharingPanel />);

        // then
        expect(screen.getByRole('heading', { name: /Have something to share\?/ }))
            .toBeInTheDocument();

        expect(screen.getByText(new RegExp('carried you through'))).toBeInTheDocument();

        expect(screen.getByRole('button', { name: /Submit a contribution/ }))
            .toBeInTheDocument();
    });

    it('should render whatever text it is handed instead', () => {
        // when
        render(
            <SharingPanel
                title="Tell us your story"
                description="Short or long, we want it."
                buttonText="Start writing" />);

        // then
        expect(screen.getByRole('heading', { name: /Tell us your story/ }))
            .toBeInTheDocument();

        expect(screen.getByText('Short or long, we want it.')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: /Start writing/ })).toBeInTheDocument();
    });

    it('should draw the icon with the css it was given, inside the heading', () => {
        // when
        const rendered = render(<SharingPanel iconCss="fa-solid fa-feather" />);

        // then: inline in the heading is what lets a narrow title wrap beneath the icon
        const heading = rendered.container.querySelector('h3');

        expect(heading?.querySelector('i.fa-solid.fa-feather')).toBeInTheDocument();
    });

    it('should default the icon to the pencil', () => {
        // when
        const rendered = render(<SharingPanel />);

        // then
        expect(rendered.container.querySelector('h3 i.bi-pencil-square')).toBeInTheDocument();
    });

    it('should raise onSubmit when the button is pressed', async () => {
        // given
        const onSubmit = vi.fn();

        render(<SharingPanel onSubmit={onSubmit} />);

        // when
        await userEvent.click(screen.getByRole('button', { name: /Submit a contribution/ }));

        // then
        expect(onSubmit).toHaveBeenCalledTimes(1);
    });

    // Wrapping button text in the narrow face would break the design's one hard rule about it.
    it('should never let the button text wrap', () => {
        // when
        render(<SharingPanel />);

        // then
        expect(screen.getByRole('button', { name: /Submit a contribution/ }))
            .toHaveClass('text-nowrap');
    });

    // The adaptation is a container query, so it keys off the PANEL's own width — the class pair
    // the stylesheet defines is the whole mechanism, and losing either silently freezes the
    // panel in one face.
    it('should carry the container and body classes the adaptation keys off', () => {
        // when
        const rendered = render(<SharingPanel />);

        // then
        expect(rendered.container.querySelector('section.g2h-sharing-panel'))
            .toBeInTheDocument();

        expect(rendered.container.querySelector('.g2h-sharing-panel-body'))
            .toBeInTheDocument();
    });
});
