using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Rendering;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace ContainerDesktop;



/// <summary>
/// A <see cref="VisualLineElementGenerator"/> that produces line elements for folded <see cref="FoldingSection"/>s.
/// </summary>
public sealed class NoBorderFoldingElementGenerator : VisualLineElementGenerator, ITextViewConnect
{
	private readonly List<TextView> textViews = new();
	private FoldingManager foldingManager;
	public static readonly Brush DefaultTextBrush = Brushes.Gray;
	
	private static readonly MethodInfo _removeFromTextViewMethod = typeof(FoldingManager).GetMethod("RemoveFromTextView", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly MethodInfo _addToTextViewMethod = typeof(FoldingManager).GetMethod("AddToTextView", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo _textViewsField = typeof(FoldingManager).GetField("textViews", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo _documentField = typeof(FoldingManager).GetField("document", BindingFlags.NonPublic | BindingFlags.Instance);
	
	#region FoldingManager property / connecting with TextView
	/// <summary>
	/// Gets/Sets the folding manager from which the foldings should be shown.
	/// </summary>
	public FoldingManager FoldingManager
	{
		get
		{
			return foldingManager;
		}
		set
		{
			if (foldingManager != value)
			{
				if (foldingManager != null)
				{
					foreach (TextView v in textViews)
					{
						_removeFromTextViewMethod.Invoke(foldingManager, new object[] { v });
					}
				}
				foldingManager = value;
				if (foldingManager != null)
				{
					foreach (TextView v in textViews)
					{
						_addToTextViewMethod.Invoke(foldingManager, new object[] { v });
					}
				}
			}
		}
	}

	void ITextViewConnect.AddToTextView(TextView textView)
	{
		textViews.Add(textView);
		if (foldingManager != null)
		{
			_addToTextViewMethod.Invoke(foldingManager, new object[] { textView });
		}
	}

	void ITextViewConnect.RemoveFromTextView(TextView textView)
	{
		textViews.Remove(textView);
		if (foldingManager != null)
		{
			_removeFromTextViewMethod.Invoke(foldingManager, new object[] { textView });
		}
	}
	#endregion

	/// <inheritdoc/>
	public override void StartGeneration(ITextRunConstructionContext context)
	{
		base.StartGeneration(context);
		if (foldingManager != null)
		{
			var textViews = (List<TextView>)_textViewsField.GetValue(foldingManager);
			if (!textViews.Contains(context.TextView))
			{
				throw new ArgumentException("Invalid TextView");
			}
			var document = (TextDocument)_documentField.GetValue(foldingManager);
			if (context.Document != document)
			{
				throw new ArgumentException("Invalid document");
			}
		}
	}

	/// <inheritdoc/>
	public override int GetFirstInterestedOffset(int startOffset)
	{
		if (foldingManager != null)
		{
			foreach (FoldingSection fs in foldingManager.GetFoldingsContaining(startOffset))
			{
				// Test whether we're currently within a folded folding (that didn't just end).
				// If so, create the fold marker immediately.
				// This is necessary if the actual beginning of the fold marker got skipped due to another VisualElementGenerator.
				if (fs.IsFolded && fs.EndOffset > startOffset)
				{
					//return startOffset;
				}
			}
			return foldingManager.GetNextFoldedFoldingStart(startOffset);
		}
		else
		{
			return -1;
		}
	}

	/// <inheritdoc/>
	public override VisualLineElement ConstructElement(int offset)
	{
		if (foldingManager == null)
		{
			return null;
		}
		int foldedUntil = -1;
		FoldingSection foldingSection = null;
		foreach (FoldingSection fs in foldingManager.GetFoldingsContaining(offset))
		{
			if (fs.IsFolded)
			{
				if (fs.EndOffset > foldedUntil)
				{
					foldedUntil = fs.EndOffset;
					foldingSection = fs;
				}
			}
		}
		if (foldedUntil > offset && foldingSection != null)
		{
			// Handle overlapping foldings: if there's another folded folding
			// (starting within the foldingSection) that continues after the end of the folded section,
			// then we'll extend our fold element to cover that overlapping folding.
			bool foundOverlappingFolding;
			do
			{
				foundOverlappingFolding = false;
				foreach (FoldingSection fs in FoldingManager.GetFoldingsContaining(foldedUntil))
				{
					if (fs.IsFolded && fs.EndOffset > foldedUntil)
					{
						foldedUntil = fs.EndOffset;
						foundOverlappingFolding = true;
					}
				}
			} while (foundOverlappingFolding);

			string title = foldingSection.Title;
			if (string.IsNullOrEmpty(title))
			{
				title = "...";
			}
			var p = new VisualLineElementTextRunProperties(CurrentContext.GlobalTextRunProperties);
			p.SetForegroundBrush(DefaultTextBrush);
			var textFormatter = TextFormatter.Create(TextOptions.GetTextFormattingMode(CurrentContext.TextView));
			var text = FormattedTextElement.PrepareText(textFormatter, title, p);
			return new FoldingLineElement(foldingSection, text, foldedUntil - offset) { _textBrush = DefaultTextBrush };
		}
		else
		{
			return null;
		}
	}

	sealed class FoldingLineElement : FormattedTextElement
	{
		readonly FoldingSection _fs;

		internal Brush _textBrush;

		public FoldingLineElement(FoldingSection fs, TextLine text, int documentLength) : base(text, documentLength)
		{
			_fs = fs;
		}

		public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
		{
			return new FormattedTextRun(this, TextRunProperties);
		}


		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Left)
			{
				_fs.IsFolded = false;
				e.Handled = true;
			}
			else
			{
				base.OnMouseDown(e);
			}
		}
	}
}
