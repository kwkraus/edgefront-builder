'use client'

import { signOut, useSession } from 'next-auth/react'
import { LogOut } from 'lucide-react'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'

function getInitials(name: string | null | undefined): string {
  if (!name) return '?'
  const parts = name.trim().split(/\s+/)
  if (parts.length === 1) return parts[0][0].toUpperCase()
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
}

export default function UserMenu() {
  const { data: session } = useSession()

  if (!session?.user) return null

  const { name, email, image } = session.user

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button className="flex items-center gap-2 rounded-full border-none bg-transparent outline-none shadow-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 [text-decoration:none]">
          <Avatar size="sm">
            {image && <AvatarImage src={image} alt={name ?? 'User avatar'} />}
            <AvatarFallback>{getInitials(name)}</AvatarFallback>
          </Avatar>
          <span className="text-sm font-medium">{name}</span>
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56">
        <DropdownMenuLabel className="font-normal">
          <div className="flex flex-col gap-1">
            <p className="text-sm font-medium leading-none">{name}</p>
            {email && (
              <p className="text-xs text-muted-foreground leading-none">{email}</p>
            )}
          </div>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem onSelect={() => signOut({ callbackUrl: '/login' })}>
          <LogOut />
          Sign out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
