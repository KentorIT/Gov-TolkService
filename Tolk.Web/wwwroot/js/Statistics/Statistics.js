$(document).ready(function () {
    setReportDateText();
});

$("body").on("change", "#ReportType", function() {
    setReportDateText();
});

function setReportDateText() {
    if ($("#ReportType option:selected").val() === "") {
        $("#ReportDateHolder").html("Typ av datum som gäller för vald rapport");
    }
    else {
        $("#ReportDateHolder").html("För denna rapport kan du göra urval med " + $("#ReportType option:selected").data('additional'));
    }
}

$("body").on("click", ".more-info-report", function () {
    
    if ($(this).text().indexOf("alla") !== -1) {
        $(this).html($(this).html().replace("alla", "färre"));
        $(this).closest(".wrap-report-info").find(".total-report").collapse("toggle");
        $(this).closest(".wrap-report-info").find(".less-report").collapse("hide");
    }
    else {
        $(this).html($(this).html().replace("färre", "alla"));
        $(this).closest(".wrap-report-info").find(".total-report").collapse("hide");
        $(this).closest(".wrap-report-info").find(".less-report").collapse("toggle");
    }
});