using Ream.Lexing;
using Ream.Interpreting;

namespace Ream.Parsing
{
   [Serializable] public abstract class Stmt
   {
     public abstract T Accept<T>(Visitor<T> visitor);
     public interface Visitor<T>
     {
         public T VisitBlockStmt(Block stmt);
         public T VisitClassStmt(Class stmt);
         public T VisitExpressionStmt(Expression stmt);
         public T VisitFunctionStmt(Function stmt);
         public T VisitMethodStmt(Method stmt);
         public T VisitIfStmt(If stmt);
         public T VisitReturnStmt(Return stmt);
         public T VisitContinueStmt(Continue stmt);
         public T VisitBreakStmt(Break stmt);
         public T VisitTypedStmt(Typed stmt);
         public T VisitWhileStmt(While stmt);
         public T VisitImportStmt(Import stmt);
         public T VisitForStmt(For stmt);
         public T VisitEvaluateStmt(Evaluate stmt);
     }
     [Serializable] public class Block : Stmt
      {
     public readonly List<Stmt> statements;

         public Block(List<Stmt> statements)
          {
             this.statements = statements;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitBlockStmt(this);
          }
      }

     [Serializable] public class Class : Stmt
      {
     public readonly Token name;
     public readonly List<Stmt.Function> functions;
     public readonly List<Stmt.Typed> variables;

         public Class(Token name, List<Stmt.Function> functions, List<Stmt.Typed> variables)
          {
             this.name = name;
             this.functions = functions;
             this.variables = variables;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitClassStmt(this);
          }
      }

     [Serializable] public class Expression : Stmt
      {
     public readonly Expr expression;

         public Expression(Expr expression)
          {
             this.expression = expression;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitExpressionStmt(this);
          }
      }

     [Serializable] public class Function : Stmt
      {
     public readonly Token name;
     public readonly VariableType type;
     public readonly List<Token> parameters;
     public readonly List<Stmt> body;

         public Function(Token name, VariableType type, List<Token> parameters, List<Stmt> body)
          {
             this.name = name;
             this.type = type;
             this.parameters = parameters;
             this.body = body;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitFunctionStmt(this);
          }
      }

     [Serializable] public class Method : Stmt
      {
     public readonly Expr obj;
     public readonly Token name;
     public readonly VariableType type;
     public readonly List<Token> parameters;
     public readonly List<Stmt> body;

         public Method(Expr obj, Token name, VariableType type, List<Token> parameters, List<Stmt> body)
          {
             this.obj = obj;
             this.name = name;
             this.type = type;
             this.parameters = parameters;
             this.body = body;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitMethodStmt(this);
          }
      }

     [Serializable] public class If : Stmt
      {
     public readonly Expr condition;
     public readonly Stmt thenBranch;
     public readonly List<(Expr,Stmt)> elifBranches;
     public readonly Stmt elseBranch;

         public If(Expr condition, Stmt thenBranch, List<(Expr,Stmt)> elifBranches, Stmt elseBranch)
          {
             this.condition = condition;
             this.thenBranch = thenBranch;
             this.elifBranches = elifBranches;
             this.elseBranch = elseBranch;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitIfStmt(this);
          }
      }

     [Serializable] public class Return : Stmt
      {
     public readonly Token keyword;
     public readonly Expr value;

         public Return(Token keyword, Expr value)
          {
             this.keyword = keyword;
             this.value = value;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitReturnStmt(this);
          }
      }

     [Serializable] public class Continue : Stmt
      {
     public readonly Token keyword;

         public Continue(Token keyword)
          {
             this.keyword = keyword;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitContinueStmt(this);
          }
      }

     [Serializable] public class Break : Stmt
      {
     public readonly Token keyword;

         public Break(Token keyword)
          {
             this.keyword = keyword;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitBreakStmt(this);
          }
      }

     [Serializable] public class Typed : Stmt
      {
     public readonly Token name;
     public readonly Expr initializer;
     public readonly VariableType type;

         public Typed(Token name, Expr initializer, VariableType type)
          {
             this.name = name;
             this.initializer = initializer;
             this.type = type;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitTypedStmt(this);
          }
      }

     [Serializable] public class While : Stmt
      {
     public readonly Expr condition;
     public readonly Stmt body;

         public While(Expr condition, Stmt body)
          {
             this.condition = condition;
             this.body = body;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitWhileStmt(this);
          }
      }

     [Serializable] public class Import : Stmt
      {
     public readonly List<Token> name;

         public Import(List<Token> name)
          {
             this.name = name;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitImportStmt(this);
          }
      }

     [Serializable] public class For : Stmt
      {
     public readonly Token name;
     public readonly Expr iterator;
     public readonly Stmt body;

         public For(Token name, Expr iterator, Stmt body)
          {
             this.name = name;
             this.iterator = iterator;
             this.body = body;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitForStmt(this);
          }
      }

     [Serializable] public class Evaluate : Stmt
      {
     public readonly Expr value;

         public Evaluate(Expr value)
          {
             this.value = value;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitEvaluateStmt(this);
          }
      }

  }
}
