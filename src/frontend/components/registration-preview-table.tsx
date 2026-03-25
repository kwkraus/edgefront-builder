'use client'

import { useState, useCallback } from 'react'
import { Button, TextInput } from '@primer/react'
import { ChevronDownIcon, ChevronUpIcon, CheckIcon, XIcon } from '@primer/octicons-react'
import type { ParsedRegistrant } from '@/lib/api/types'

interface RegistrationPreviewTableProps {
  registrants: ParsedRegistrant[]
  onEditRegistrant: (index: number, updated: ParsedRegistrant) => void
  showFailedOnly?: boolean
}

interface EditingState {
  index: number
  email: string
  firstName: string
  lastName: string
}

export function RegistrationPreviewTable({
  registrants,
  onEditRegistrant,
  showFailedOnly = false,
}: RegistrationPreviewTableProps) {
  const [expandedIndex, setExpandedIndex] = useState<number | null>(null)
  const [editingIndex, setEditingIndex] = useState<number | null>(null)
  const [editingState, setEditingState] = useState<EditingState | null>(null)

  const displayRegistrants = showFailedOnly
    ? registrants.filter((r) => r.status === 'failed')
    : registrants

  const handleStartEdit = useCallback((index: number, registrant: ParsedRegistrant) => {
    // Find the actual index in the full registrants array
    const actualIndex = registrants.findIndex((r) => r === registrant)
    setEditingIndex(actualIndex)
    setEditingState({
      index: actualIndex,
      email: registrant.email,
      firstName: registrant.firstName,
      lastName: registrant.lastName,
    })
  }, [registrants])

  const handleSaveEdit = useCallback(() => {
    if (!editingState) return

    const registrant = registrants[editingState.index]
    const updated: ParsedRegistrant = {
      ...registrant,
      email: editingState.email.trim(),
      firstName: editingState.firstName.trim(),
      lastName: editingState.lastName.trim(),
      status: 'success', // Mark as fixed when manually edited
      errorReason: null,
    }

    onEditRegistrant(editingState.index, updated)
    setEditingIndex(null)
    setEditingState(null)
  }, [editingState, registrants, onEditRegistrant])

  const handleCancelEdit = useCallback(() => {
    setEditingIndex(null)
    setEditingState(null)
  }, [])

  const handleToggleExpand = useCallback((index: number) => {
    setExpandedIndex(expandedIndex === index ? null : index)
  }, [expandedIndex])

  return (
    <div className="overflow-x-auto rounded-lg border" style={{ borderColor: 'var(--borderColor-default)' }}>
      <table className="w-full text-sm">
        <thead>
          <tr style={{ backgroundColor: 'var(--bgColor-muted, var(--color-canvas-subtle))' }}>
            <th className="px-4 py-2 text-left font-semibold" style={{ width: '40px' }} />
            <th className="px-4 py-2 text-left font-semibold">Email</th>
            <th className="px-4 py-2 text-left font-semibold">First Name</th>
            <th className="px-4 py-2 text-left font-semibold">Last Name</th>
            <th className="px-4 py-2 text-left font-semibold" style={{ width: '100px' }}>
              Status
            </th>
            <th className="px-4 py-2 text-left font-semibold" style={{ width: '100px' }}>
              Action
            </th>
          </tr>
        </thead>
        <tbody>
          {displayRegistrants.map((registrant, displayIndex) => {
            const actualIndex = registrants.indexOf(registrant)
            const isEditing = editingIndex === actualIndex
            const isFailed = registrant.status === 'failed'

            if (isEditing && editingState) {
              return (
                <tr
                  key={actualIndex}
                  style={{
                    backgroundColor: 'var(--bgColor-accent-muted, var(--color-accent-subtle))',
                    borderTop: '1px solid var(--borderColor-default)',
                    borderBottom: '1px solid var(--borderColor-default)',
                  }}
                >
                  <td className="px-4 py-3" colSpan={6}>
                    <div className="space-y-3">
                      <h4 className="font-semibold text-sm">Edit Registrant #{displayIndex + 1}</h4>
                      <div className="grid gap-3 grid-cols-3">
                        <div>
                          <label className="block text-xs font-medium mb-1">Email *</label>
                          <TextInput
                            value={editingState.email}
                            onChange={(e) =>
                              setEditingState({ ...editingState, email: e.target.value })
                            }
                            placeholder="name@example.com"
                            block
                          />
                        </div>
                        <div>
                          <label className="block text-xs font-medium mb-1">First Name *</label>
                          <TextInput
                            value={editingState.firstName}
                            onChange={(e) =>
                              setEditingState({
                                ...editingState,
                                firstName: e.target.value,
                              })
                            }
                            placeholder="John"
                            block
                          />
                        </div>
                        <div>
                          <label className="block text-xs font-medium mb-1">Last Name *</label>
                          <TextInput
                            value={editingState.lastName}
                            onChange={(e) =>
                              setEditingState({
                                ...editingState,
                                lastName: e.target.value,
                              })
                            }
                            placeholder="Doe"
                            block
                          />
                        </div>
                      </div>
                      <div className="flex gap-2 justify-end">
                        <Button size="small" variant="primary" onClick={handleSaveEdit}>
                          Save
                        </Button>
                        <Button size="small" variant="default" onClick={handleCancelEdit}>
                          Cancel
                        </Button>
                      </div>
                    </div>
                  </td>
                </tr>
              )
            }

            return (
              <tr
                key={actualIndex}
                style={{
                  backgroundColor: isFailed ? 'rgba(248, 113, 113, 0.1)' : 'transparent',
                  borderTop: '1px solid var(--borderColor-default)',
                }}
              >
                <td className="px-4 py-3">
                  <button
                    onClick={() => handleToggleExpand(actualIndex)}
                    className="inline-flex items-center justify-center p-1"
                    aria-label="Toggle details"
                    disabled={!isFailed}
                  >
                    {isFailed &&
                      (expandedIndex === actualIndex ? (
                        <ChevronUpIcon size={16} />
                      ) : (
                        <ChevronDownIcon size={16} />
                      ))}
                  </button>
                </td>
                <td className="px-4 py-3">{registrant.email}</td>
                <td className="px-4 py-3">{registrant.firstName}</td>
                <td className="px-4 py-3">{registrant.lastName}</td>
                <td className="px-4 py-3">
                  <span
                    className={`inline-flex items-center gap-1 text-xs font-medium px-2 py-1 rounded`}
                    style={{
                      backgroundColor:
                        registrant.status === 'success'
                          ? 'var(--bgColor-success-muted, var(--color-success-subtle))'
                          : 'var(--bgColor-danger-muted, var(--color-danger-subtle))',
                      color:
                        registrant.status === 'success'
                          ? 'var(--fgColor-success, var(--color-success-fg))'
                          : 'var(--fgColor-danger, var(--color-danger-fg))',
                    }}
                  >
                    {registrant.status === 'success' ? (
                      <>
                        <CheckIcon size={12} />
                        Success
                      </>
                    ) : (
                      <>
                        <XIcon size={12} />
                        Failed
                      </>
                    )}
                  </span>
                </td>
                <td className="px-4 py-3">
                  {isFailed && (
                    <Button
                      size="small"
                      variant="default"
                      onClick={() => handleStartEdit(actualIndex, registrant)}
                    >
                      Edit
                    </Button>
                  )}
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>

      {displayRegistrants.length === 0 && (
        <div className="text-center py-8" style={{ color: 'var(--fgColor-muted)' }}>
          <p className="text-sm">No registrants to display</p>
        </div>
      )}
    </div>
  )
}
