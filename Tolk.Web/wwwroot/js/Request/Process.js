$(function () {
    $('#InterpreterId').change(function () {
        if ($(this).val() === "-1") {
            $('#new-interpreter').collapse('show');
        }
        else {
            $('#new-interpreter').collapse('hide');
        }
    });
});