import { Link, useRouterState } from "@tanstack/react-router";
import {
  LayoutDashboard,
  Users,
  UserRound,
  BarChart3,
  HeartHandshake,
  GraduationCap,
  ClipboardList,
  Upload,
} from "lucide-react";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/components/ui/sidebar";

const items = [
  { title: "Painel", url: "/", icon: LayoutDashboard },
  { title: "Turmas", url: "/turmas", icon: Users },
  { title: "Alunos", url: "/alunos", icon: UserRound },
  { title: "Notas", url: "/notas", icon: ClipboardList },
  { title: "Tutoria", url: "/tutoria", icon: HeartHandshake },
  { title: "Comparativos", url: "/comparativos", icon: BarChart3 },
  { title: "Importar", url: "/importar", icon: Upload },
];

export function AppSidebar() {
  const pathname = useRouterState({ select: (r) => r.location.pathname });

  const isActive = (url: string) =>
    url === "/" ? pathname === "/" : pathname.startsWith(url);

  return (
    <Sidebar collapsible="icon">
      <SidebarHeader className="border-b border-sidebar-border">
        <div className="flex items-center gap-3 px-2 py-3">
          <div className="flex h-9 w-9 items-center justify-center rounded-md bg-gold text-gold-foreground">
            <GraduationCap className="h-5 w-5" />
          </div>
          <div className="flex flex-col leading-tight group-data-[collapsible=icon]:hidden">
            <span className="font-display text-base font-semibold text-sidebar-foreground">
              Regente
            </span>
            <span className="text-xs text-sidebar-foreground/60">
              Caderno do professor
            </span>
          </div>
        </div>
      </SidebarHeader>

      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              {items.map((item) => (
                <SidebarMenuItem key={item.url}>
                  <SidebarMenuButton asChild isActive={isActive(item.url)} tooltip={item.title}>
                    <Link to={item.url}>
                      <item.icon className="h-4 w-4" />
                      <span>{item.title}</span>
                    </Link>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>

      <SidebarFooter className="border-t border-sidebar-border">
        <div className="px-3 py-2 text-[11px] uppercase tracking-wider text-sidebar-foreground/50 group-data-[collapsible=icon]:hidden">
          v1 · local
        </div>
      </SidebarFooter>
    </Sidebar>
  );
}
