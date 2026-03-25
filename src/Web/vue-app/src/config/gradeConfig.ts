export type GradeLetter = "A" | "B" | "C" | "D" | "E" | "F";

export const GRADE_VALUES: Record<GradeLetter, number> = {
  A: 100,
  B: 75,
  C: 60,
  D: 40,
  E: 10,
  F: 0,
};

export const ALL_GRADES: GradeLetter[] = ["A", "B", "C", "D", "E", "F"];
export const THREE_GRADES: GradeLetter[] = ["A", "C", "E"];

export function numericToLetter(value: number): GradeLetter {
  if (value >= 87.5) return "A";
  if (value >= 67.5) return "B";
  if (value >= 50) return "C";
  if (value >= 25) return "D";
  if (value >= 5) return "E";
  return "F";
}
