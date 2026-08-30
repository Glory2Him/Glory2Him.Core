import { describe, expect, it } from 'vitest';
import { NavArea } from '../../../models/views/navigations/navItem';
import { navMenuProvider } from './navMenuProvider';

describe('navMenuProvider.resolveArea', () => {
    it('should resolve account paths to the Account area', () => {
        expect(navMenuProvider.resolveArea('Account/Manage')).toBe(NavArea.Account);
    });

    it('should ignore leading slashes when resolving the area', () => {
        expect(navMenuProvider.resolveArea('/Account/Manage/Email')).toBe(NavArea.Account);
    });

    it('should resolve account paths case-insensitively', () => {
        expect(navMenuProvider.resolveArea('account/manage')).toBe(NavArea.Account);
    });

    it('should resolve the dashboard to the Admin area', () => {
        expect(navMenuProvider.resolveArea('Dashboard')).toBe(NavArea.Admin);
    });

    it('should resolve admin paths to the Admin area', () => {
        expect(navMenuProvider.resolveArea('/Admin/Users')).toBe(NavArea.Admin);
    });

    it('should resolve an empty path to the Admin area', () => {
        expect(navMenuProvider.resolveArea('')).toBe(NavArea.Admin);
    });
});

describe('navMenuProvider.getNavMenu', () => {
    it('should return the full menu when no path is given', () => {
        // when
        const menu = navMenuProvider.getNavMenu();

        // then
        const titles = menu.map((item) => item.title);
        expect(titles).toEqual(['Dashboard', 'Admin', 'Sample Pages', 'My Account']);
    });

    it('should return only Account sections for an account path', () => {
        // when
        const menu = navMenuProvider.getNavMenu('/Account/Manage');

        // then
        expect(menu).toHaveLength(1);
        expect(menu[0].title).toBe('My Account');
        expect(menu[0].area).toBe(NavArea.Account);
    });

    it('should return only Admin sections for an admin path', () => {
        // when
        const menu = navMenuProvider.getNavMenu('/Admin/Users');

        // then
        const titles = menu.map((item) => item.title);
        expect(titles).toEqual(['Dashboard', 'Admin', 'Sample Pages']);
        menu.forEach((item) => expect(item.area ?? NavArea.Admin).toBe(NavArea.Admin));
    });

    it('should treat the dashboard path as the Admin area', () => {
        // when
        const menu = navMenuProvider.getNavMenu('Dashboard');

        // then
        expect(menu.map((item) => item.title)).toContain('Dashboard');
        expect(menu.map((item) => item.title)).not.toContain('My Account');
    });
});

describe('navMenuProvider Admin section', () => {
    const getAdminSection = () =>
        navMenuProvider.getNavMenu().find((item) => item.title === 'Admin');

    it('should list content item settings below users', () => {
        // when
        const children = getAdminSection()?.children ?? [];

        // then
        expect(children.map((child) => child.title))
            .toEqual(['Users', 'Content Item Settings', 'Posts']);
    });

    it('should point each admin entry at its route', () => {
        // when
        const children = getAdminSection()?.children ?? [];

        // then: these must match adminRoutes, or the sidebar links land on nothing
        expect(children.map((child) => child.href))
            .toEqual(['Admin/Users', 'Admin/ContentItemSettings', 'Admin/Posts']);
    });

    it('should restrict every admin entry to administrators', () => {
        // when
        const children = getAdminSection()?.children ?? [];

        // then
        children.forEach((child) => {
            expect(child.roles).toEqual(['Administrators']);
            expect(child.requiresAuth).toBe(true);
        });
    });
});

describe('navMenuProvider.getSamplePagesSection', () => {
    it('should expose the same sample pages section the sidebar uses', () => {
        // when
        const section = navMenuProvider.getSamplePagesSection();
        const menu = navMenuProvider.getNavMenu('/Admin');

        // then
        expect(section.title).toBe('Sample Pages');
        expect(menu.map((item) => item.title)).toContain(section.title);
    });

    it('should offer a Components group listing every documented component', () => {
        // when
        const section = navMenuProvider.getSamplePagesSection();

        const components = (section.children ?? [])
            .find((child) => child.title === 'Components');

        // then
        expect(components).toBeDefined();

        expect((components?.children ?? []).map((child) => child.title)).toEqual([
            'Association Panel',
            'Tag Association Panel',
            'Bible Reference Association Panel',
            'Review Panel',
            'Content Item Panel'
        ]);
    });

    it('should point each component entry at its reference page', () => {
        // when
        const components = (navMenuProvider.getSamplePagesSection().children ?? [])
            .find((child) => child.title === 'Components');

        // then: these must match samplePagesRoutes, or the sidebar links land on nothing
        expect((components?.children ?? []).map((child) => child.href)).toEqual([
            'SamplePages/Components/Association-Panel',
            'SamplePages/Components/Tag-Association-Panel',
            'SamplePages/Components/Bible-Reference-Association-Panel',
            'SamplePages/Components/Review-Panel',
            'SamplePages/Components/Content-Item-Panel'
        ]);
    });
});
