import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, BaseRouteReuseStrategy, Params } from "@angular/router";

/**
 * Angular reuses a routed component when only its route params change (/projects/1 → /projects/2), so a page that
 * reads its ID once in ngOnInit keeps showing the old record. Routes that opt in with
 * `data: { remountOnParamChange: true }` get a fresh component instead (NPT-1125 rework: header search jumping
 * between two records of the same type). Every other route keeps the default reuse behavior.
 */
@Injectable()
export class RemountOnParamChangeReuseStrategy extends BaseRouteReuseStrategy {
    override shouldReuseRoute(future: ActivatedRouteSnapshot, curr: ActivatedRouteSnapshot): boolean {
        if (future.routeConfig === curr.routeConfig && future.routeConfig?.data?.["remountOnParamChange"]) {
            return paramsAreEqual(future.params, curr.params);
        }
        return super.shouldReuseRoute(future, curr);
    }
}

function paramsAreEqual(a: Params, b: Params): boolean {
    const keys = Object.keys(a);
    return keys.length === Object.keys(b).length && keys.every((key) => a[key] === b[key]);
}
