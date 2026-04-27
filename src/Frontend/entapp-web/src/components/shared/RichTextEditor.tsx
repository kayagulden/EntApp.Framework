"use client";

import { useEditor, EditorContent, type Editor } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Highlight from "@tiptap/extension-highlight";
import Placeholder from "@tiptap/extension-placeholder";
import Link from "@tiptap/extension-link";
import Underline from "@tiptap/extension-underline";
import TextAlign from "@tiptap/extension-text-align";
import TaskList from "@tiptap/extension-task-list";
import TaskItem from "@tiptap/extension-task-item";
import Image from "@tiptap/extension-image";
import { Table } from "@tiptap/extension-table";
import { TableRow } from "@tiptap/extension-table-row";
import { TableCell } from "@tiptap/extension-table-cell";
import { TableHeader } from "@tiptap/extension-table-header";
import { Color } from "@tiptap/extension-color";
import { TextStyle } from "@tiptap/extension-text-style";
import {
  Bold, Italic, Underline as UnderlineIcon, Strikethrough,
  Code, Heading1, Heading2, Heading3,
  List, ListOrdered, CheckSquare,
  Quote, Minus, Undo2, Redo2,
  Link as LinkIcon, Image as ImageIcon,
  AlignLeft, AlignCenter, AlignRight,
  Highlighter, Table as TableIcon,
  Pilcrow,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useCallback, useEffect, useState } from "react";

// ── Types ────────────────────────────────────────────
export interface RichTextEditorProps {
  content?: string; // JSON string
  onUpdate?: (json: string, html: string) => void;
  placeholder?: string;
  editable?: boolean;
  minHeight?: string;
  maxHeight?: string;
  className?: string;
  toolbarClassName?: string;
  /** Compact mode — fewer toolbar items */
  compact?: boolean;
}

// ── Toolbar Button ──────────────────────────────────
function ToolbarButton({
  active,
  disabled,
  onClick,
  title,
  children,
}: {
  active?: boolean;
  disabled?: boolean;
  onClick: () => void;
  title: string;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      title={title}
      className={cn(
        "p-1.5 rounded-md transition-all duration-150",
        active
          ? "bg-indigo-500/20 text-indigo-400 shadow-sm"
          : "text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-white/5",
        disabled && "opacity-30 cursor-not-allowed"
      )}
    >
      {children}
    </button>
  );
}

function ToolbarDivider() {
  return <div className="w-px h-5 bg-[var(--color-border)] mx-1" />;
}

// ── Toolbar ─────────────────────────────────────────
function EditorToolbar({ editor, compact }: { editor: Editor; compact?: boolean }) {
  const [showLinkInput, setShowLinkInput] = useState(false);
  const [linkUrl, setLinkUrl] = useState("");

  const setLink = useCallback(() => {
    if (linkUrl) {
      editor.chain().focus().extendMarkRange("link").setLink({ href: linkUrl }).run();
    }
    setShowLinkInput(false);
    setLinkUrl("");
  }, [editor, linkUrl]);

  const addImage = useCallback(() => {
    const url = window.prompt("Görsel URL'si:");
    if (url) editor.chain().focus().setImage({ src: url }).run();
  }, [editor]);

  const addTable = useCallback(() => {
    editor.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run();
  }, [editor]);

  return (
    <div className="flex flex-wrap items-center gap-0.5 px-3 py-2 border-b border-[var(--color-border)] bg-[var(--color-card-bg)]/50 backdrop-blur-sm rounded-t-xl">
      {/* Text formatting */}
      <ToolbarButton active={editor.isActive("bold")} onClick={() => editor.chain().focus().toggleBold().run()} title="Kalın">
        <Bold className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton active={editor.isActive("italic")} onClick={() => editor.chain().focus().toggleItalic().run()} title="İtalik">
        <Italic className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton active={editor.isActive("underline")} onClick={() => editor.chain().focus().toggleUnderline().run()} title="Altı çizili">
        <UnderlineIcon className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton active={editor.isActive("strike")} onClick={() => editor.chain().focus().toggleStrike().run()} title="Üstü çizili">
        <Strikethrough className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton active={editor.isActive("highlight")} onClick={() => editor.chain().focus().toggleHighlight().run()} title="Vurgula">
        <Highlighter className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton active={editor.isActive("code")} onClick={() => editor.chain().focus().toggleCode().run()} title="Satır içi kod">
        <Code className="w-4 h-4" />
      </ToolbarButton>

      <ToolbarDivider />

      {/* Headings */}
      <ToolbarButton active={editor.isActive("heading", { level: 1 })} onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()} title="Başlık 1">
        <Heading1 className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton active={editor.isActive("heading", { level: 2 })} onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()} title="Başlık 2">
        <Heading2 className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton active={editor.isActive("heading", { level: 3 })} onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()} title="Başlık 3">
        <Heading3 className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton active={editor.isActive("paragraph")} onClick={() => editor.chain().focus().setParagraph().run()} title="Paragraf">
        <Pilcrow className="w-4 h-4" />
      </ToolbarButton>

      <ToolbarDivider />

      {/* Lists */}
      <ToolbarButton active={editor.isActive("bulletList")} onClick={() => editor.chain().focus().toggleBulletList().run()} title="Madde listesi">
        <List className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton active={editor.isActive("orderedList")} onClick={() => editor.chain().focus().toggleOrderedList().run()} title="Numaralı liste">
        <ListOrdered className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton active={editor.isActive("taskList")} onClick={() => editor.chain().focus().toggleTaskList().run()} title="Kontrol listesi">
        <CheckSquare className="w-4 h-4" />
      </ToolbarButton>

      <ToolbarDivider />

      {/* Block elements */}
      <ToolbarButton active={editor.isActive("blockquote")} onClick={() => editor.chain().focus().toggleBlockquote().run()} title="Alıntı">
        <Quote className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton onClick={() => editor.chain().focus().setHorizontalRule().run()} title="Yatay çizgi">
        <Minus className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton active={editor.isActive("codeBlock")} onClick={() => editor.chain().focus().toggleCodeBlock().run()} title="Kod bloğu">
        <Code className="w-4 h-4" />
      </ToolbarButton>

      {!compact && (
        <>
          <ToolbarDivider />

          {/* Alignment */}
          <ToolbarButton active={editor.isActive({ textAlign: "left" })} onClick={() => editor.chain().focus().setTextAlign("left").run()} title="Sola hizala">
            <AlignLeft className="w-4 h-4" />
          </ToolbarButton>
          <ToolbarButton active={editor.isActive({ textAlign: "center" })} onClick={() => editor.chain().focus().setTextAlign("center").run()} title="Ortala">
            <AlignCenter className="w-4 h-4" />
          </ToolbarButton>
          <ToolbarButton active={editor.isActive({ textAlign: "right" })} onClick={() => editor.chain().focus().setTextAlign("right").run()} title="Sağa hizala">
            <AlignRight className="w-4 h-4" />
          </ToolbarButton>

          <ToolbarDivider />

          {/* Media & Table */}
          <div className="relative">
            <ToolbarButton active={editor.isActive("link")} onClick={() => setShowLinkInput(!showLinkInput)} title="Bağlantı">
              <LinkIcon className="w-4 h-4" />
            </ToolbarButton>
            {showLinkInput && (
              <div className="absolute top-full left-0 mt-1 z-50 flex items-center gap-1 p-1.5 rounded-lg bg-[var(--color-card-bg)] border border-[var(--color-border)] shadow-xl">
                <input
                  type="url"
                  placeholder="https://..."
                  value={linkUrl}
                  onChange={(e) => setLinkUrl(e.target.value)}
                  onKeyDown={(e) => e.key === "Enter" && setLink()}
                  className="px-2 py-1 text-xs rounded bg-[var(--color-input-bg)] border border-[var(--color-border)] text-[var(--color-text)] w-48 focus:outline-none focus:ring-1 focus:ring-indigo-500/40"
                  autoFocus
                />
                <button onClick={setLink} className="px-2 py-1 text-xs bg-indigo-600 text-white rounded hover:bg-indigo-700 transition-colors">Ekle</button>
              </div>
            )}
          </div>
          <ToolbarButton onClick={addImage} title="Görsel">
            <ImageIcon className="w-4 h-4" />
          </ToolbarButton>
          <ToolbarButton onClick={addTable} title="Tablo ekle">
            <TableIcon className="w-4 h-4" />
          </ToolbarButton>
        </>
      )}

      <ToolbarDivider />

      {/* Undo/Redo */}
      <ToolbarButton disabled={!editor.can().undo()} onClick={() => editor.chain().focus().undo().run()} title="Geri al">
        <Undo2 className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton disabled={!editor.can().redo()} onClick={() => editor.chain().focus().redo().run()} title="İleri al">
        <Redo2 className="w-4 h-4" />
      </ToolbarButton>
    </div>
  );
}

// ── Main Component ──────────────────────────────────
export function RichTextEditor({
  content,
  onUpdate,
  placeholder = "İçerik yazın...",
  editable = true,
  minHeight = "200px",
  maxHeight = "600px",
  className,
  toolbarClassName,
  compact = false,
}: RichTextEditorProps) {
  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        heading: { levels: [1, 2, 3, 4] },
        codeBlock: false, // use lowlight version in future
      }),
      Highlight,
      Underline,
      Link.configure({ openOnClick: false, autolink: true }),
      Placeholder.configure({ placeholder }),
      TextAlign.configure({ types: ["heading", "paragraph"] }),
      TaskList,
      TaskItem.configure({ nested: true }),
      Image.configure({ inline: false }),
      Table.configure({ resizable: true }),
      TableRow,
      TableCell,
      TableHeader,
      Color,
      TextStyle,
    ],
    content: content ? tryParseJson(content) : "",
    editable,
    onUpdate: ({ editor }) => {
      const json = JSON.stringify(editor.getJSON());
      const html = editor.getHTML();
      onUpdate?.(json, html);
    },
    editorProps: {
      attributes: {
        class: cn(
          "prose prose-invert prose-sm max-w-none focus:outline-none px-4 py-3",
          "prose-headings:text-[var(--color-text)] prose-headings:font-semibold",
          "prose-p:text-[var(--color-text-muted)] prose-p:leading-relaxed",
          "prose-a:text-indigo-400 prose-a:no-underline hover:prose-a:underline",
          "prose-code:text-pink-400 prose-code:bg-pink-500/10 prose-code:px-1.5 prose-code:py-0.5 prose-code:rounded-md prose-code:text-xs",
          "prose-pre:bg-[#0d1117] prose-pre:border prose-pre:border-[var(--color-border)] prose-pre:rounded-lg",
          "prose-blockquote:border-l-indigo-500 prose-blockquote:text-[var(--color-text-muted)]",
          "prose-img:rounded-lg prose-img:shadow-lg",
          "prose-table:border-collapse",
          "prose-th:bg-[var(--color-card-bg)] prose-th:border prose-th:border-[var(--color-border)] prose-th:px-3 prose-th:py-2 prose-th:text-xs prose-th:font-semibold",
          "prose-td:border prose-td:border-[var(--color-border)] prose-td:px-3 prose-td:py-2 prose-td:text-sm",
          "prose-hr:border-[var(--color-border)]",
          "prose-ul:list-disc prose-ol:list-decimal",
          "[&_ul[data-type=taskList]]:list-none [&_ul[data-type=taskList]]:pl-0",
          "[&_ul[data-type=taskList]_li]:flex [&_ul[data-type=taskList]_li]:items-start [&_ul[data-type=taskList]_li]:gap-2",
          "[&_ul[data-type=taskList]_li_label]:mt-0.5",
        ),
      },
    },
    immediatelyRender: false,
  });

  // Sync external content changes
  useEffect(() => {
    if (!editor || !content) return;
    const parsed = tryParseJson(content);
    if (parsed && JSON.stringify(editor.getJSON()) !== JSON.stringify(parsed)) {
      editor.commands.setContent(parsed);
    }
  }, [content, editor]);

  // Sync editable
  useEffect(() => {
    if (editor) editor.setEditable(editable);
  }, [editable, editor]);

  if (!editor) {
    return (
      <div className={cn("rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] animate-pulse", className)} style={{ minHeight }}>
        <div className="h-10 border-b border-[var(--color-border)] bg-white/5 rounded-t-xl" />
        <div className="p-4 space-y-3">
          <div className="h-4 w-3/4 bg-white/5 rounded" />
          <div className="h-4 w-1/2 bg-white/5 rounded" />
          <div className="h-4 w-5/6 bg-white/5 rounded" />
        </div>
      </div>
    );
  }

  return (
    <div className={cn("rounded-xl border border-[var(--color-border)] bg-[var(--color-card-bg)] overflow-hidden transition-all", className)}>
      {editable && <EditorToolbar editor={editor} compact={compact} />}
      <div style={{ minHeight, maxHeight, overflowY: "auto" }}>
        <EditorContent editor={editor} />
      </div>
    </div>
  );
}

// ── Read-only Viewer ────────────────────────────────
export function RichTextViewer({
  content,
  className,
}: {
  content?: string;
  className?: string;
}) {
  return (
    <RichTextEditor
      content={content}
      editable={false}
      className={className}
      minHeight="auto"
      maxHeight="none"
    />
  );
}

// ── Utilities ───────────────────────────────────────
function tryParseJson(content: string): Record<string, unknown> | string {
  try {
    const parsed = JSON.parse(content);
    if (typeof parsed === "object" && parsed !== null) return parsed;
    return content;
  } catch {
    return content;
  }
}

export default RichTextEditor;
