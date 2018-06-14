$(function () {
    $('#InterpreterId').change(function () {
        if ($(this).val() === "-1") {
            $('#new-interpreter').collapse('show');
        }
        else {
            $('#new-interpreter').collapse('hide');
        }
    });
    $('#InterpreterLocation').change(function () {
        if ($(this).val() === "OffSite") {
            $('#set-expected-travel-costs').collapse('hide');
        }
        else {
            $('#set-expected-travel-costs').collapse('show');
        }
    });
});