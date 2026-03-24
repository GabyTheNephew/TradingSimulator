export enum ChartRange {
    OneYear = 'OneYear',
    OneMonth = 'OneMonth',
    OneDay = 'OneDay'
}

export function getRangePriority(range: ChartRange): number {
  switch (range) {
    case ChartRange.OneYear: return 6;
    case ChartRange.OneMonth: return 5;
    case ChartRange.OneDay: return 4;
    default: return 5;
  }
}