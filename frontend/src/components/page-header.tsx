import type { ReactNode } from "react";

export function PageHeader({
  eyebrow,
  title,
  description,
  actions,
}: {
  eyebrow?: string;
  title: string;
  description?: string;
  actions?: ReactNode;
}) {
  return (
    <div className="flex flex-col gap-5 border-b border-border/70 pb-8 sm:flex-row sm:items-end sm:justify-between">
      <div className="max-w-3xl">
        {eyebrow && (
          <div className="flex items-center gap-3">
            <span className="gold-rule" />
            <span className="eyebrow">{eyebrow}</span>
          </div>
        )}
        <h1 className="headline mt-3 text-4xl text-foreground sm:text-5xl">
          {title}
        </h1>
        {description && (
          <p className="mt-3 max-w-2xl text-[15px] leading-relaxed text-muted-foreground">
            {description}
          </p>
        )}
      </div>
      {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
    </div>
  );
}
