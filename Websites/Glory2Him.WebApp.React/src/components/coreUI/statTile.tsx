import { StatTileVariant } from '../../models/coreUI/statTileVariant';
import './coreUI.css';

// CoreUI StatTile, restyled as the Blogzine dashboard "counter" card so it matches the
// template's look and feel. Same API as the EventHighway original (Variant/Value/Label/Icon).
export interface StatTileProps {
    variant?: StatTileVariant;
    value?: string;
    label?: string;
    icon?: string;
}

// The RAG variants map onto Blogzine's Bootstrap contextual colours so the tile matches the
// template's dashboard counters rather than EventHighway's gradient RAG styling.
const variantCssClasses: Record<StatTileVariant, string> = {
    Green: 'rag-green',
    Amber: 'rag-amber',
    Red: 'rag-red',
    Na: 'rag-na',
};

const iconCssClasses: Record<StatTileVariant, string> = {
    Green: 'bg-success bg-opacity-10 text-success',
    Amber: 'bg-warning bg-opacity-10 text-warning',
    Red: 'bg-danger bg-opacity-10 text-danger',
    Na: 'bg-primary bg-opacity-10 text-primary',
};

export function StatTile({ variant = 'Na', value, label, icon }: StatTileProps) {
    return (
        <div className={`card card-body border p-3 stat-tile ${variantCssClasses[variant]}`}>
            <div className="d-flex align-items-center">
                {icon != null && (
                    <div className={`icon-xl fs-1 rounded-3 stat-tile-icon ${iconCssClasses[variant]}`}>
                        <i className={`bi ${icon}`}></i>
                    </div>
                )}
                <div className="ms-3">
                    <h3 className="mb-0 stat-tile-value">{value}</h3>
                    <h6 className="mb-0 stat-tile-label">{label}</h6>
                </div>
            </div>
        </div>
    );
}
