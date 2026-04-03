export function twMerge(...inputs) {
  return inputs.filter(Boolean).join(" ");
}

export function extendTailwindMerge() {
  return twMerge;
}
