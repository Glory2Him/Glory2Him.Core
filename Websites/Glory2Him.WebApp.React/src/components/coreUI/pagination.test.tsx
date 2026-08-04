import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { Pagination } from './pagination';

describe('Pagination', () => {
    it('should render one numbered button per page in the default variant', () => {
        // when
        render(<Pagination currentPage={2} totalPages={3} />);

        // then
        expect(screen.getByRole('button', { name: '1' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: '2' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: '3' })).toBeInTheDocument();
        expect(screen.queryByRole('button', { name: 'Prev' })).not.toBeInTheDocument();
    });

    it('should invoke onPageChange with the clicked page number', async () => {
        // given
        const user = userEvent.setup();
        const onPageChange = vi.fn();
        render(<Pagination currentPage={1} totalPages={3} onPageChange={onPageChange} />);

        // when
        await user.click(screen.getByRole('button', { name: '3' }));

        // then
        expect(onPageChange).toHaveBeenCalledTimes(1);
        expect(onPageChange).toHaveBeenCalledWith(3);
    });

    it('should not invoke onPageChange when clicking the current page', async () => {
        // given
        const user = userEvent.setup();
        const onPageChange = vi.fn();
        render(<Pagination currentPage={2} totalPages={3} onPageChange={onPageChange} />);

        // when
        await user.click(screen.getByRole('button', { name: '2' }));

        // then
        expect(onPageChange).not.toHaveBeenCalled();
    });

    it('should disable the previous control on the first page', () => {
        // when
        render(<Pagination currentPage={1} totalPages={3} variant="PrevNext" />);

        // then
        expect(screen.getByRole('button', { name: 'Prev' })).toBeDisabled();
        expect(screen.getByRole('button', { name: 'Next' })).toBeEnabled();
    });

    it('should disable the next control on the last page', () => {
        // when
        render(<Pagination currentPage={3} totalPages={3} variant="PrevNext" />);

        // then
        expect(screen.getByRole('button', { name: 'Next' })).toBeDisabled();
        expect(screen.getByRole('button', { name: 'Prev' })).toBeEnabled();
    });

    it('should step to the adjacent pages in the PrevNext variant', async () => {
        // given
        const user = userEvent.setup();
        const onPageChange = vi.fn();
        render(
            <Pagination
                currentPage={2}
                totalPages={3}
                variant="PrevNext"
                onPageChange={onPageChange} />);

        // when
        await user.click(screen.getByRole('button', { name: 'Prev' }));
        await user.click(screen.getByRole('button', { name: 'Next' }));

        // then
        expect(onPageChange).toHaveBeenNthCalledWith(1, 1);
        expect(onPageChange).toHaveBeenNthCalledWith(2, 3);
    });

    it('should hide page numbers in the PrevNext variant', () => {
        // when
        render(<Pagination currentPage={2} totalPages={3} variant="PrevNext" />);

        // then
        expect(screen.queryByRole('button', { name: '2' })).not.toBeInTheDocument();
    });

    it('should apply the rounded css class in the Rounded variant', () => {
        // when
        render(<Pagination currentPage={1} totalPages={2} variant="Rounded" />);

        // then
        expect(screen.getByRole('list')).toHaveClass('pagination-rounded');
    });

    it('should mark the current page item as active', () => {
        // when
        render(<Pagination currentPage={2} totalPages={3} />);

        // then
        const activeItem = screen.getByRole('button', { name: '2' }).closest('li');
        expect(activeItem).toHaveClass('active');
    });

    it('should apply the aria label to the navigation landmark', () => {
        // when
        render(<Pagination ariaLabel="Blog pages" totalPages={2} />);

        // then
        expect(screen.getByRole('navigation', { name: 'Blog pages' })).toBeInTheDocument();
    });
});
