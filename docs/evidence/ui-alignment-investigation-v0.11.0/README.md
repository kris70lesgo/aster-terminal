# UI candidate failure retained

An inactive-tab descendant selector was initially placed directly inside a
ControlTheme. The application built, but its new desktop regression rejected
that scope when the template loaded. The descendant rule belongs in the normal
application Styles collection; the template-part rule remains in the theme.
The failing local TRX is retained. No release archive from that candidate was
published. Final native acceptance must use the corrected source.
