$.validator.addMethod('requiredif', function (value, element, params) {
    var otherElement = params[0];
    switch (params[1]) {
        case "notnull":
            return $(otherElement).val() !== null || $(otherElement).val() !== undefined || $(otherElement).val() !== '';
        case "System.Boolean":
            var booleanValue = params['value'] === 'True' ? true : false;
            return otherElement.checked === booleanValue;
        case "System.Integer":
        case "System.Long":
        case "System.Float":
        case "System.Double":
            return Number($(otherElement).val()) === Number(params['value']);
        case "System.String":
        default:
            return $(otherElement).val() === params['value'];
    }
});

$.validator.unobtrusive.adapters.add('requiredif', ['otherproperty', 'otherpropertytype', 'value'], function (options) {
    var element = $(options.form).find("#" + options.params['otherproperty'])[0];
    options.rules['requiredif'] = [element, options.params['otherpropertytype'], options.params['value']];
    options.messages['requiredif'] = options.message;
});
