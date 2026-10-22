# Orange County Stormwater Tools — [Release Date] Update: Modernized Inventory Management & AI-Powered WQMP Extraction

We are excited to share a significant update to the Orange County Stormwater Tools. This release continues the modernization of the application, bringing the Inventory, Data Hub, and Delineation modules into a faster, more consistent experience alongside the Planning and Trash modules. It also marks the full launch of the AI-powered WQMP extraction workflow for all jurisdictions. We welcome your feedback as you explore the new features.

---

## WQMP AI Extraction Workflow — Now Live for All Jurisdictions

- Upload a WQMP PDF and let the AI pre-fill plan details automatically — reducing manual data entry and the risk of transcription errors
- Covers all major plan sections: general information, Quick BMPs, Treatment BMPs, and Source Control BMPs
- Works with both current WQMP templates and older South Orange County formats
- Source Control BMPs are organized into three categories: Site Design, Non-Structural Controls, and Structural Source Controls
- Each extracted value is color-coded so you can tell at a glance whether it came from an existing record, was pulled from the document by AI, or was entered manually
- Click any AI-extracted value to jump directly to the page in the PDF where it was found
- The AI extraction tool can also be used on existing WQMP records — if a plan was previously entered with limited detail, uploading the source PDF will propose values for missing or incomplete fields, and users can choose which suggested values to accept or override

---

## Inventory & BMP Management

- The Treatment BMP list now combines a searchable, filterable data grid with an interactive map in a single view — select a BMP on the map to highlight it in the list, and vice versa
- Use the "Zoom to My Location" button on the map to quickly center the view on your current position
- Start a field visit directly from the BMP list using the actions menu — no need to navigate to the BMP detail page first
- Jurisdiction Managers can control the public visibility of their BMP inventory directly from their Jurisdiction detail page
- The public-facing view respects each jurisdiction's visibility setting — showing only verified BMPs for jurisdictions set to "Verified Only," and no BMPs for jurisdictions set to "None"
- Treatment BMP Type detail pages now show all BMPs of that type in a grid — with the ability to download the full attribute list directly from the page
- The Manager Dashboard brings Provisional BMPs, Provisional Delineations, and Provisional Field Visits together in a new tabbed layout
- Bulk-verify multiple provisional records at once directly from the dashboard

---

## Field Visits & O&M

- The Field Visit workflow has been fully modernized — we welcome feedback from staff using tablets or cellular connections on whether performance has improved
- The View All Field Visits page has been reorganized into a new tabbed layout
- The WQMP O&M Report form has been redesigned as a guided multi-step workflow
- New read-only Field Visit Detail page for reviewing past visits without entering edit mode
- Assessment photos are now displayed in a tile layout and are also visible in the BMP detail photo carousel

---

## Delineation Map

- Delineation Map fully rebuilt in the modernized application — draw, edit, verify, and delete drainage areas without navigating to a separate page
- When drawing delineation boundaries, features now snap to nearby geometries for more accurate polygon edges
- Left-side panel shows BMP details and all delineation actions when a BMP is selected
- Regional Subbasin Revision Request workflow migrated to the modernized application
- Delineation GDB upload, staging review, and download are all available in the modernized application

---

## Data Hub & Bulk Tools

- Data Hub page migrated to the modernized application with four tabs: BMPs & Delineations, WQMPs, Trash Module, County GIS Integration
- All major bulk upload and download tools now available in the new interface
- The Land Use Block GDB uploader now includes a preview and approval step before data is committed — validation errors are shown immediately in the upload form making it easier to catch and fix issues before data goes live
- Across all Data Hub uploaders, error messages have been improved to identify the specific row and field that failed, including the WQMP, Field Visit, OVTA, and Simplified BMP upload tools
- A dedicated external API with interactive documentation is now available for jurisdictions connecting to OCST data from Power BI or other analysis tools — including updated access token management accessible from the API tab in the Data Hub

---

## Simplified URLs

- The Planning Module and Trash Module are now accessible at the main application URL — the separate `-web.` address is no longer needed
- URLs across the Inventory module have been streamlined — for example, the Treatment BMP list is now at `ocstormwatertools.org/treatment-bmps` rather than the previous longer path
- Top-level bookmarks will redirect automatically — users with bookmarks to deeper pages may need to update them

---

As always, we appreciate your partnership in making the Orange County Stormwater Tools work better for your team. If you have questions about any of the new features or run into anything unexpected, please reach out to your ESA project contact. We look forward to hearing your feedback.

---

<!-- TO DO BEFORE SENDING:
  - Fill in release date in the headline
  - Confirm redirect behavior for deeper bookmarks
  - Add External API URL if desired
-->
