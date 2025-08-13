using System.Web;

namespace Shortly.API.HTMLTemplates;

public class PasswordFormTemplate
{
    public static string Generate(string? errorMessage = null, string token = "")
    {
        var error = !string.IsNullOrEmpty(errorMessage)
            ? $"<div class='error'>{HttpUtility.HtmlEncode(errorMessage)}</div>"
            : string.Empty;

        return $@"
            <!DOCTYPE html>
            <html lang='en'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Password Required</title>
                <style>
                    body {{
                        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                        min-height: 100vh;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        margin: 0;
                        padding: 20px;
                        box-sizing: border-box;
                    }}
                    .container {{
                        background: rgba(255, 255, 255, 0.95);
                        padding: 40px;
                        border-radius: 20px;
                        box-shadow: 0 20px 40px rgba(0, 0, 0, 0.1);
                        backdrop-filter: blur(10px);
                        border: 1px solid rgba(255, 255, 255, 0.2);
                        max-width: 400px;
                        width: 100%;
                        text-align: center;
                    }}
                    h1 {{
                        color: #333;
                        margin-bottom: 10px;
                        font-size: 28px;
                        font-weight: 600;
                    }}
                    p {{
                        color: #666;
                        margin-bottom: 30px;
                        line-height: 1.5;
                    }}
                    .form-group {{
                        margin-bottom: 25px;
                        text-align: left;
                    }}
                    label {{
                        display: block;
                        margin-bottom: 8px;
                        font-weight: 500;
                        color: #333;
                    }}
                    input[type='password'] {{
                        width: 100%;
                        padding: 15px;
                        border: 2px solid #e1e5e9;
                        border-radius: 10px;
                        font-size: 16px;
                        transition: all 0.3s ease;
                        box-sizing: border-box;
                    }}
                    input[type='password']:focus {{
                        outline: none;
                        border-color: #667eea;
                        box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
                    }}
                    button {{
                        width: 100%;
                        padding: 15px;
                        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                        color: white;
                        border: none;
                        border-radius: 10px;
                        font-size: 16px;
                        font-weight: 600;
                        cursor: pointer;
                        transition: all 0.3s ease;
                        text-transform: uppercase;
                        letter-spacing: 0.5px;
                    }}
                    button:hover {{
                        transform: translateY(-2px);
                        box-shadow: 0 10px 25px rgba(102, 126, 234, 0.3);
                    }}
                    button:active {{
                        transform: translateY(0);
                    }}
                    .error {{
                        background: #fee;
                        color: #c33;
                        padding: 12px;
                        border-radius: 8px;
                        margin-bottom: 20px;
                        border: 1px solid #fcc;
                        font-size: 14px;
                    }}
                    .short-code {{
                        background: #f8f9fa;
                        padding: 8px 12px;
                        border-radius: 6px;
                        font-family: 'Courier New', monospace;
                        color: #495057;
                        font-weight: bold;
                    }}
                    @media (max-width: 480px) {{
                        .container {{
                            padding: 30px 20px;
                            margin: 10px;
                        }}
                        h1 {{
                            font-size: 24px;
                        }}
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h1>🔒 Password Required</h1>
                    <p>The short URL is password protected.</p>
                    {error}
                    <form method='post' action='/api/url-redirect/verify'>
                    <input type='hidden' name='token' value='{HttpUtility.HtmlEncode(token)}'>
                    <div class='form-group'>
                        <label for='password'>Enter Password:</label>
                        <input type='password' id='password' name='password' required autofocus 
                               placeholder='Enter the password to continue'>
                    </div>
                    <button type='submit'>Continue</button>
                </form>
                </div>
                <script>
                    document.getElementById('password').focus();
                    document.addEventListener('DOMContentLoaded', function() {{
                        document.querySelector('form').addEventListener('submit', function(e) {{
                            const password = document.getElementById('password').value;
                            if (!password.trim()) {{
                                e.preventDefault();
                                alert('Please enter a password');
                            }}
                        }});
                    }});
                </script>
            </body>
            </html>";
    }
}