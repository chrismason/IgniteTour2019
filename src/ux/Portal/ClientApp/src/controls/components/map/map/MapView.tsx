import * as React from "react";
import { Map, loadModules, WebMap } from "@esri/react-arcgis";
import { UXOLayer } from "../layers/UXOLayer";
import { ISelectedItem } from "../../../../models/Map/ISelectedItem";
import { IMapItem } from "../../../../models/Map/IMapItem";

export interface IMapViewProps {
    uxoMapItems: IMapItem[] | null;

    onItemSelected(item: ISelectedItem): void;
}

interface IMapState {
    mapLayers: any[];
}

export class MapView extends React.Component<IMapViewProps, IMapState> {
    constructor(props: IMapViewProps) {
        super(props);
    }

    render() {
        return (
            <div className="mapView">
                <Map
                    mapProperties={{
                        basemap: "satellite"
                    }}
                    viewProperties={{
                        center: [-98.585522, 39.833333],
                        zoom: 5
                    }}
                >
                    {this.renderUXOLayer()}
                </Map>
            </div>
        );
    }

    private renderUXOLayer() {
        let layer = null;
        if (this.props.uxoMapItems && this.props.uxoMapItems.length > 0) {
            layer = (
                <UXOLayer
                    onUxoSelected={this.onItemSelected}
                    items={this.props.uxoMapItems}
                />
            );
        }
        return layer;
    }

    private onItemSelected = (item: ISelectedItem) => {
        this.props.onItemSelected(item);
    }
}