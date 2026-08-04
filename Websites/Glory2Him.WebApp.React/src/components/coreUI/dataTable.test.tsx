import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { DataTableColumn } from '../../models/coreUI/dataTableColumn';
import { DataTable } from './dataTable';

interface Person {
    name: string;
    age: number;
}

const columns: ReadonlyArray<DataTableColumn<Person>> = [
    { title: 'Name', value: (person) => person.name },
    { title: 'Age', value: (person) => person.age }
];

const people: ReadonlyArray<Person> = [
    { name: 'Anna', age: 34 },
    { name: 'Ben', age: 28 },
    { name: 'Chloe', age: 41 },
    { name: 'Daniel', age: 19 },
    { name: 'Esther', age: 52 }
];

const getBodyRows = () => {
    const table = screen.getByRole('table');
    const [, body] = within(table).getAllByRole('rowgroup');

    return within(body).queryAllByRole('row');
};

const getFirstCellTexts = () =>
    getBodyRows().map((row) => within(row).getAllByRole('cell')[0].textContent);

describe('DataTable', () => {
    it('should render every item when it fits on one page', () => {
        // when
        render(<DataTable items={people} columns={columns} />);

        // then
        expect(getBodyRows()).toHaveLength(5);
        expect(screen.queryByText(/Page \d+ of \d+/)).not.toBeInTheDocument();
    });

    it('should filter rows case-insensitively across all columns', async () => {
        // given
        const user = userEvent.setup();
        render(<DataTable items={people} columns={columns} />);

        // when
        await user.type(screen.getByPlaceholderText('Search...'), 'aN');

        // then
        expect(getFirstCellTexts()).toEqual(['Anna', 'Daniel']);
    });

    it('should match the search term against numeric column values', async () => {
        // given
        const user = userEvent.setup();
        render(<DataTable items={people} columns={columns} />);

        // when
        await user.type(screen.getByPlaceholderText('Search...'), '52');

        // then
        expect(getFirstCellTexts()).toEqual(['Esther']);
    });

    it('should hide the search box when searchable is false', () => {
        // when
        render(<DataTable items={people} columns={columns} searchable={false} />);

        // then
        expect(screen.queryByPlaceholderText('Search...')).not.toBeInTheDocument();
    });

    it('should sort ascending on first header click and descending on second', async () => {
        // given
        const user = userEvent.setup();
        render(<DataTable items={people} columns={columns} />);

        // when
        await user.click(screen.getByRole('button', { name: 'Age' }));

        // then
        expect(getFirstCellTexts()).toEqual(
            ['Daniel', 'Ben', 'Anna', 'Chloe', 'Esther']);

        // when
        await user.click(screen.getByRole('button', { name: 'Age' }));

        // then
        expect(getFirstCellTexts()).toEqual(
            ['Esther', 'Chloe', 'Anna', 'Ben', 'Daniel']);
    });

    it('should page items and clamp navigation at the bounds', async () => {
        // given
        const user = userEvent.setup();
        render(<DataTable items={people} columns={columns} pageSize={2} />);
        const previousButton = () => screen.getByRole('button', { name: 'Previous' });
        const nextButton = () => screen.getByRole('button', { name: 'Next' });

        // then - first page
        expect(getFirstCellTexts()).toEqual(['Anna', 'Ben']);
        expect(screen.getByText('Page 1 of 3')).toBeInTheDocument();
        expect(previousButton()).toBeDisabled();

        // when - walk to the last page
        await user.click(nextButton());
        await user.click(nextButton());

        // then - last page is clamped
        expect(getFirstCellTexts()).toEqual(['Esther']);
        expect(screen.getByText('Page 3 of 3')).toBeInTheDocument();
        expect(nextButton()).toBeDisabled();
    });

    it('should reset to page one when the search narrows the results', async () => {
        // given
        const user = userEvent.setup();
        render(<DataTable items={people} columns={columns} pageSize={2} />);
        await user.click(screen.getByRole('button', { name: 'Next' }));
        expect(screen.getByText('Page 2 of 3')).toBeInTheDocument();

        // when
        await user.type(screen.getByPlaceholderText('Search...'), 'chloe');

        // then
        expect(getFirstCellTexts()).toEqual(['Chloe']);
        expect(screen.queryByText(/Page \d+ of \d+/)).not.toBeInTheDocument();
    });

    it('should render row actions in a trailing cell', () => {
        // when
        render(
            <DataTable
                items={people.slice(0, 1)}
                columns={columns}
                rowActions={(person) => <button type="button">Edit {person.name}</button>} />);

        // then
        expect(screen.getByRole('button', { name: 'Edit Anna' })).toBeInTheDocument();
    });
});
