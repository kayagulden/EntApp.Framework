"use client";

import { use } from "react";
import { DynamicPage } from "@/components/dynamic/DynamicPage";

interface DynamicEntityPageProps {
  params: Promise<{ entityName: string }>;
}

/**
 * Dynamic route: /dashboard/dynamic/[entityName]
 * URL'deki entityName parametresi ile DynamicPage render eder.
 */
export default function DynamicEntityPage({ params }: DynamicEntityPageProps) {
  const { entityName } = use(params);

  return <DynamicPage entityName={entityName} />;
}
