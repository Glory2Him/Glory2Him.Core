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

describe('navMenuProvider.getSamplePagesSection', () => {
    it('should expose the same sample pages section the sidebar uses', () => {
        // when
        const section = navMenuProvider.getSamplePagesSection();
        const menu = navMenuProvider.getNavMenu('/Admin');

        // then
        expect(section.title).toBe('Sample Pages');
        expect(menu.map((item) => item.title)).toContain(section.title);
    });
});
