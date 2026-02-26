export type GradeLetter = "A" | "B" | "C" | "D" | "E";

export const GRADE_VALUES: Record<GradeLetter, number> = {
  A: 100,
  B: 85,
  C: 70,
  D: 55,
  E: 40,
};

export const ALL_GRADES: GradeLetter[] = ["A", "B", "C", "D", "E"];
export const THREE_GRADES: GradeLetter[] = ["A", "C", "E"];

export function numericToLetter(value: number): GradeLetter {
  if (value >= 92.5) return "A";
  if (value >= 77.5) return "B";
  if (value >= 62.5) return "C";
  if (value >= 47.5) return "D";
  return "E";
}
