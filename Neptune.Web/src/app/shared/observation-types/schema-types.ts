import { ObservationTypeSpecificationEnum } from "src/app/shared/generated/enum/observation-type-specification-enum";
import { ObservationTypeCollectionMethodEnum } from "src/app/shared/generated/enum/observation-type-collection-method-enum";
import { ObservationTargetTypeEnum } from "src/app/shared/generated/enum/observation-target-type-enum";
import { ObservationThresholdTypeEnum } from "src/app/shared/generated/enum/observation-threshold-type-enum";

export type CollectionMethodType = "PassFail" | "DiscreteValue" | "Percentage" | "Rate";

export function getCollectionMethod(specID: number): CollectionMethodType | null {
    const triple = specToTriple(specID);
    if (!triple) return null;
    return collectionMethodIDToName(triple.CollectionMethodID);
}

export function collectionMethodIDToName(id: number | null | undefined): CollectionMethodType | null {
    switch (id) {
        case ObservationTypeCollectionMethodEnum.PassFail: return "PassFail";
        case ObservationTypeCollectionMethodEnum.DiscreteValue: return "DiscreteValue";
        case ObservationTypeCollectionMethodEnum.Percentage: return "Percentage";
        default: return null;
    }
}

// Reverse + forward mapping between (CollectionMethod, Target, Threshold) and the persisted spec ID.
// Backend column stays ObservationTypeSpecificationID; the modal exposes 3 separate dropdowns.
export interface SpecTriple {
    CollectionMethodID: number;
    TargetTypeID: number;
    ThresholdTypeID: number;
}

const SPEC_TRIPLES: ReadonlyArray<{ specID: ObservationTypeSpecificationEnum; triple: SpecTriple }> = [
    { specID: ObservationTypeSpecificationEnum.PassFail_PassFail_None,
        triple: { CollectionMethodID: ObservationTypeCollectionMethodEnum.PassFail, TargetTypeID: ObservationTargetTypeEnum.PassFail, ThresholdTypeID: ObservationThresholdTypeEnum.None } },
    { specID: ObservationTypeSpecificationEnum.DiscreteValues_HighTargetValue_DiscreteThresholdValue,
        triple: { CollectionMethodID: ObservationTypeCollectionMethodEnum.DiscreteValue, TargetTypeID: ObservationTargetTypeEnum.High, ThresholdTypeID: ObservationThresholdTypeEnum.SpecificValue } },
    { specID: ObservationTypeSpecificationEnum.DiscreteValues_HighTargetValue_PercentFromBenchmark,
        triple: { CollectionMethodID: ObservationTypeCollectionMethodEnum.DiscreteValue, TargetTypeID: ObservationTargetTypeEnum.High, ThresholdTypeID: ObservationThresholdTypeEnum.RelativeToBenchmark } },
    { specID: ObservationTypeSpecificationEnum.DiscreteValues_LowTargetValue_DiscreteThresholdValue,
        triple: { CollectionMethodID: ObservationTypeCollectionMethodEnum.DiscreteValue, TargetTypeID: ObservationTargetTypeEnum.Low, ThresholdTypeID: ObservationThresholdTypeEnum.SpecificValue } },
    { specID: ObservationTypeSpecificationEnum.DiscreteValues_LowTargetValue_PercentFromBenchmark,
        triple: { CollectionMethodID: ObservationTypeCollectionMethodEnum.DiscreteValue, TargetTypeID: ObservationTargetTypeEnum.Low, ThresholdTypeID: ObservationThresholdTypeEnum.RelativeToBenchmark } },
    { specID: ObservationTypeSpecificationEnum.DiscreteValues_SpecificTargetValue_DiscreteThresholdValue,
        triple: { CollectionMethodID: ObservationTypeCollectionMethodEnum.DiscreteValue, TargetTypeID: ObservationTargetTypeEnum.SpecificValue, ThresholdTypeID: ObservationThresholdTypeEnum.SpecificValue } },
    { specID: ObservationTypeSpecificationEnum.DiscreteValues_SpecificTargetValue_PercentFromBenchmark,
        triple: { CollectionMethodID: ObservationTypeCollectionMethodEnum.DiscreteValue, TargetTypeID: ObservationTargetTypeEnum.SpecificValue, ThresholdTypeID: ObservationThresholdTypeEnum.RelativeToBenchmark } },
    { specID: ObservationTypeSpecificationEnum.PercentValue_HighTargetValue_DiscreteThresholdValue,
        triple: { CollectionMethodID: ObservationTypeCollectionMethodEnum.Percentage, TargetTypeID: ObservationTargetTypeEnum.High, ThresholdTypeID: ObservationThresholdTypeEnum.SpecificValue } },
    { specID: ObservationTypeSpecificationEnum.PercentValue_HighTargetValue_PercentFromBenchmark,
        triple: { CollectionMethodID: ObservationTypeCollectionMethodEnum.Percentage, TargetTypeID: ObservationTargetTypeEnum.High, ThresholdTypeID: ObservationThresholdTypeEnum.RelativeToBenchmark } },
    { specID: ObservationTypeSpecificationEnum.PercentValue_LowTargetValue_DiscreteThresholdValue,
        triple: { CollectionMethodID: ObservationTypeCollectionMethodEnum.Percentage, TargetTypeID: ObservationTargetTypeEnum.Low, ThresholdTypeID: ObservationThresholdTypeEnum.SpecificValue } },
    { specID: ObservationTypeSpecificationEnum.PercentValue_LowTargetValue_PercentFromBenchmark,
        triple: { CollectionMethodID: ObservationTypeCollectionMethodEnum.Percentage, TargetTypeID: ObservationTargetTypeEnum.Low, ThresholdTypeID: ObservationThresholdTypeEnum.RelativeToBenchmark } },
    { specID: ObservationTypeSpecificationEnum.PercentValue_SpecificTargetValue_DiscreteThresholdValue,
        triple: { CollectionMethodID: ObservationTypeCollectionMethodEnum.Percentage, TargetTypeID: ObservationTargetTypeEnum.SpecificValue, ThresholdTypeID: ObservationThresholdTypeEnum.SpecificValue } },
    { specID: ObservationTypeSpecificationEnum.PercentValue_SpecificTargetValue_PercentFromBenchmark,
        triple: { CollectionMethodID: ObservationTypeCollectionMethodEnum.Percentage, TargetTypeID: ObservationTargetTypeEnum.SpecificValue, ThresholdTypeID: ObservationThresholdTypeEnum.RelativeToBenchmark } },
];

export function tripleToSpec(triple: Partial<SpecTriple>): ObservationTypeSpecificationEnum | null {
    if (triple.CollectionMethodID == null || triple.TargetTypeID == null || triple.ThresholdTypeID == null) return null;
    const match = SPEC_TRIPLES.find((s) =>
        s.triple.CollectionMethodID === triple.CollectionMethodID &&
        s.triple.TargetTypeID === triple.TargetTypeID &&
        s.triple.ThresholdTypeID === triple.ThresholdTypeID,
    );
    return match ? match.specID : null;
}

export function specToTriple(specID: number | null | undefined): SpecTriple | null {
    if (specID == null) return null;
    const match = SPEC_TRIPLES.find((s) => s.specID === specID);
    return match ? { ...match.triple } : null;
}

export interface DiscreteValueSchema {
    MeasurementUnitLabel: string;
    MeasurementUnitTypeID: number;
    PropertiesToObserve: string[];
    MinimumNumberOfObservations: number;
    MaximumNumberOfObservations?: number;
    MinimumValueOfObservations: number;
    MaximumValueOfObservations?: number;
    BenchmarkDescription: string;
    ThresholdDescription: string;
    AssessmentDescription: string;
}

export interface PassFailSchema {
    PropertiesToObserve: string[];
    AssessmentDescription: string;
    PassingScoreLabel: string;
    FailingScoreLabel: string;
}

export interface PercentageSchema {
    MeasurementUnitLabel: string;
    PropertiesToObserve: string[];
    BenchmarkDescription: string;
    ThresholdDescription: string;
    AssessmentDescription: string;
}

export interface RateSchema {
    DiscreteRateMeasurementUnitLabel: string;
    DiscreteRateMeasurementUnitTypeID: number;
    TimeMeasurementUnitLabel: string;
    TimeMeasurementUnitTypeID: number;
    ReadingMeasurementUnitLabel: string;
    ReadingMeasurementUnitTypeID: number;
    PropertiesToObserve: string[];
    DiscreteRateMinimumNumberOfObservations: number;
    DiscreteRateMaximumNumberOfObservations?: number;
    DiscreteRateMinimumValueOfObservations: number;
    DiscreteRateMaximumValueOfObservations?: number;
    TimeReadingMinimumNumberOfObservations: number;
    TimeReadingMaximumNumberOfObservations?: number;
    TimeReadingMinimumValueOfObservations: number;
    TimeReadingMaximumValueOfObservations?: number;
    BenchmarkDescription: string;
    ThresholdDescription: string;
    AssessmentDescription: string;
}

export function emptyDiscreteValueSchema(): DiscreteValueSchema {
    return { MeasurementUnitLabel: "", MeasurementUnitTypeID: undefined, PropertiesToObserve: [], MinimumNumberOfObservations: 1, MinimumValueOfObservations: 0, BenchmarkDescription: "", ThresholdDescription: "", AssessmentDescription: "" };
}

export function emptyPassFailSchema(): PassFailSchema {
    return { PropertiesToObserve: [], AssessmentDescription: "", PassingScoreLabel: "Passes", FailingScoreLabel: "Fails" };
}

export function emptyPercentageSchema(): PercentageSchema {
    return { MeasurementUnitLabel: "", PropertiesToObserve: [], BenchmarkDescription: "", ThresholdDescription: "", AssessmentDescription: "" };
}
