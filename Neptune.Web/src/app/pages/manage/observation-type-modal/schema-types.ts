import { ObservationTypeSpecificationEnum } from "src/app/shared/generated/enum/observation-type-specification-enum";

export type CollectionMethodType = "PassFail" | "DiscreteValue" | "Percentage" | "Rate";

export function getCollectionMethod(specID: number): CollectionMethodType | null {
    if (specID === ObservationTypeSpecificationEnum.PassFail_PassFail_None) return "PassFail";
    if (specID >= 2 && specID <= 7) return "DiscreteValue";
    if (specID >= 8 && specID <= 13) return "Percentage";
    return null; // Rate specs don't exist in current seed data
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
